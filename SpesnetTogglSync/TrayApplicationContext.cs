using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using SpesnetTogglSync.Models;
using SpesnetTogglSync.Services;
using SpesnetTogglSync.SpesnetApi;
using SpesnetTogglSync.TogglApi;

namespace SpesnetTogglSync;

internal sealed class TrayApplicationContext : ApplicationContext
{
    private readonly ConfigService _configService;
    private readonly FileLogger _logger;
    private readonly SyncActivity _syncActivity;
    private readonly NotifyIcon _tray;
    private readonly Icon _waitingIcon;
    private readonly Icon _syncedIcon;
    private readonly Icon _problemIcon;
    private readonly ContextMenuStrip _menu;
    private readonly ToolStripMenuItem _statusItem;
    private readonly ToolStripMenuItem _syncNowItem;
    private readonly ToolStripMenuItem _cancelItem;
    private readonly System.Windows.Forms.Timer _timer;
    private readonly Control _ui = new();
    private readonly EventWaitHandle _showWindowEvent;
    private readonly RegisteredWaitHandle _showWindowRegistration;
    private readonly CancellationTokenSource _appShutdown = new();

    private SyncForm? _form;
    private bool _exitRequested;
    private bool _toldUserAboutTray;
    private int _scheduleCheck;
    private string? _lastScheduleError;
    private NoticeKind _notice = NoticeKind.None;

    public TrayApplicationContext(bool startInTray, EventWaitHandle showWindowEvent)
    {
        _ = _ui.Handle;
        _showWindowEvent = showWindowEvent;

        var dataDirectory = ConfigService.ResolveDataDirectory();
        _configService = new ConfigService(dataDirectory);
        _logger = new FileLogger(dataDirectory);
        _syncActivity = new SyncActivity();

        var settings = _configService.LoadSettings();
        if (!WindowsStartup.TrySetEnabled(settings.IsRunAtStartupEnabled(), out var startupError))
        {
            _logger.Warn($"Could not update Start with Windows: {startupError}");
        }

        var syncTime = DailySyncTime.Format(DailySyncTime.Parse(settings.DailySyncTime));
        _logger.Info(settings.IsRunAtStartupEnabled()
            ? $"Notification area started. Daily sync at {syncTime} SAST, or as soon as this PC is on after that. Start with Windows is on."
            : $"Notification area started. Daily sync at {syncTime} SAST, or as soon as this PC is on after that. Start with Windows is off.");

        _waitingIcon = CreateIcon(Color.FromArgb(214, 132, 0), DrawClock);
        _syncedIcon = CreateIcon(Color.FromArgb(22, 140, 68), DrawCheck);
        _problemIcon = CreateIcon(Color.FromArgb(192, 40, 36), DrawBang);

        _statusItem = new ToolStripMenuItem($"Daily sync at {syncTime}") { Enabled = false };
        var openItem = new ToolStripMenuItem("Open", null, (_, _) => ShowMainForm());
        _syncNowItem = new ToolStripMenuItem("Sync now", null, OnSyncNow);
        _cancelItem = new ToolStripMenuItem("Cancel today's sync", null, (_, _) => CancelToday());
        var exitItem = new ToolStripMenuItem("Exit", null, (_, _) => ExitApplication());

        _menu = new ContextMenuStrip();
        _menu.Items.Add(_statusItem);
        _menu.Items.Add(new ToolStripSeparator());
        _menu.Items.Add(openItem);
        _menu.Items.Add(_syncNowItem);
        _menu.Items.Add(_cancelItem);
        _menu.Items.Add(new ToolStripSeparator());
        _menu.Items.Add(exitItem);

        _tray = new NotifyIcon
        {
            Icon = _waitingIcon,
            Text = $"Daily sync at {syncTime}",
            Visible = true,
            ContextMenuStrip = _menu
        };
        _tray.MouseDoubleClick += (_, e) =>
        {
            if (e.Button == MouseButtons.Left)
            {
                ShowMainForm();
            }
        };
        _tray.BalloonTipClicked += (_, _) => OnBalloonTipClicked();

        _showWindowRegistration = ThreadPool.RegisterWaitForSingleObject(
            _showWindowEvent,
            static (state, _) =>
            {
                var self = (TrayApplicationContext)state!;
                self.Post(self.ShowMainForm);
            },
            this,
            Timeout.Infinite,
            false);

        _timer = new System.Windows.Forms.Timer { Interval = 15_000 };
        _timer.Tick += OnTimerTick;
        _timer.Start();

        UpdateTrayStatus();

        if (!startInTray)
        {
            ShowMainForm();
        }

        _ui.BeginInvoke(new Action(() => _ = CheckScheduleAsync()));
    }

    private async void OnTimerTick(object? sender, EventArgs e)
    {
        await CheckScheduleAsync();
    }

    private async Task CheckScheduleAsync()
    {
        if (Interlocked.Exchange(ref _scheduleCheck, 1) == 1)
        {
            return;
        }

        var scheduleFailed = false;
        try
        {
            var settings = _configService.LoadSettings();
            var syncTime = DailySyncTime.Parse(settings.DailySyncTime);
            var nowSa = SouthAfricaClock.Now();
            var today = nowSa.ToString("yyyy-MM-dd");
            var state = _configService.LoadAutoSyncState();

            if (state.CancelledDate == today || state.CompletedDate == today)
            {
                return;
            }

            if (nowSa.TimeOfDay < syncTime)
            {
                return;
            }

            _logger.Info(
                $"Daily sync starting at {nowSa:HH:mm} SAST (scheduled {DailySyncTime.Format(syncTime)}, or as soon as the PC is on after that).");
            if (await RunScheduledSyncAsync())
            {
                MarkCompleted(today);
            }
        }
        catch (Exception ex)
        {
            scheduleFailed = true;
            _logger.Error($"Daily sync schedule failed: {ex.Message}");
            if (!string.Equals(_lastScheduleError, ex.Message, StringComparison.Ordinal))
            {
                _lastScheduleError = ex.Message;
                NotifyIssue($"Cannot sync: {ex.Message}");
            }
        }
        finally
        {
            if (!scheduleFailed)
            {
                _lastScheduleError = null;
            }

            UpdateTrayStatus();
            Interlocked.Exchange(ref _scheduleCheck, 0);
        }
    }

    private async Task<bool> RunScheduledSyncAsync()
    {
        if (!_syncActivity.TryBegin())
        {
            return false;
        }

        UpdateTrayStatus();
        try
        {
            _form?.SetSyncBusy(true);
            _logger.Info("Automatic sync starting.");
            var result = await ExecuteSyncAsync(_appShutdown.Token);
            if (_exitRequested)
            {
                return false;
            }

            await EnsureBillingAsync(result);
            PublishResult(result);
            return true;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            if (!_exitRequested)
            {
                _form?.ApplySyncProgress(new SyncProgressEventArgs { Message = "Sync failed." });
                NotifyIssue(ex.Message);
                await TryBillingAfterFailureAsync();
            }

            return !_exitRequested;
        }
        finally
        {
            _form?.SetSyncBusy(false);
            _syncActivity.End();
        }
    }

    private async void OnSyncNow(object? sender, EventArgs e)
    {
        if (!_syncActivity.TryBegin())
        {
            NotifyInfo("Sync", "A sync is already in progress.");
            return;
        }

        UpdateTrayStatus();
        var consumeAutomaticSlot = IsAutomaticSyncPending();
        try
        {
            _form?.SetSyncBusy(true);
            var result = await ExecuteSyncAsync(_appShutdown.Token);
            await EnsureBillingAsync(result);
            PublishResult(result);

            if (consumeAutomaticSlot && !_exitRequested)
            {
                MarkCompleted(SouthAfricaClock.TodayString());
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            _form?.ApplySyncProgress(new SyncProgressEventArgs { Message = "Sync failed." });
            NotifyIssue(ex.Message);
            if (!_exitRequested)
            {
                await TryBillingAfterFailureAsync();
            }

            if (consumeAutomaticSlot && !_exitRequested)
            {
                MarkCompleted(SouthAfricaClock.TodayString());
            }
        }
        finally
        {
            _form?.SetSyncBusy(false);
            _syncActivity.End();
            UpdateTrayStatus();
        }
    }

    private async Task<SyncResult> ExecuteSyncAsync(CancellationToken cancellationToken)
    {
        if (FormHasUnsavedMappings())
        {
            return Fail("Cannot sync: unsaved mapping changes. Save mappings on the Mapping tab, then sync again.");
        }

        var settings = _configService.LoadSettings();
        if (string.IsNullOrWhiteSpace(settings.TogglApiToken))
        {
            return Fail("Cannot sync: configure the Toggl API token on the Settings tab.");
        }

        if (TryGetWatermarkUtc() is not DateTime watermarkUtc)
        {
            return Fail("Cannot sync: set Sync from in the app before the first automatic sync.");
        }

        using var togglClient = new TogglApiClient(settings.TogglApiToken, _logger);
        var me = await togglClient.GetMeAsync(cancellationToken);
        var mappings = _configService.LoadMappings();
        var userMappings = _configService.GetOrCreateUserMappings(mappings, me.Id);

        var cache = settings.SpesnetReferenceCache ?? new SpesnetReferenceCache();
        if (cache.Projects.Count == 0 || cache.WorkTasks.Count == 0)
        {
            _logger.Info("Loading Spesnet reference data before sync.");
            using var refreshClient = CreateSpesnetClient(settings);
            cache = await refreshClient.RefreshReferenceDataAsync(cancellationToken);
            settings.SpesnetReferenceCache = cache;
            _configService.SaveSettings(settings);
            _form?.ApplyReferenceCache(cache);
        }

        if (cache.Projects.Count == 0 || cache.WorkTasks.Count == 0)
        {
            return Fail("Cannot sync: refresh Spesnet reference data before syncing.");
        }

        using var spesnetClient = CreateSpesnetClient(settings);
        var syncService = new SyncService(togglClient, spesnetClient, _configService, _logger);
        void OnProgress(object? _, SyncProgressEventArgs progress) => _form?.ApplySyncProgress(progress);
        syncService.Progress += OnProgress;
        try
        {
            var result = await syncService.SyncAsync(watermarkUtc, userMappings, cache, cancellationToken);
            await new BillingService(_configService, _logger).ApplyAsync(togglClient, result, cancellationToken);
            return result;
        }
        finally
        {
            syncService.Progress -= OnProgress;
        }
    }

    private async Task EnsureBillingAsync(SyncResult result)
    {
        if (result.BillingNotice != null || _exitRequested)
        {
            return;
        }

        var settings = _configService.LoadSettings();
        if (string.IsNullOrWhiteSpace(settings.TogglApiToken))
        {
            return;
        }

        using var togglClient = new TogglApiClient(settings.TogglApiToken, _logger);
        await new BillingService(_configService, _logger).ApplyAsync(togglClient, result, _appShutdown.Token);
    }

    private async Task TryBillingAfterFailureAsync()
    {
        try
        {
            await EnsureBillingAsync(new SyncResult { Success = false, Message = "Sync failed." });
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            _logger.Error($"Billing summary failed: {ex.Message}");
        }
    }

    private ISpesnetTimekeepingClient CreateSpesnetClient(AppSettings settings) =>
        settings.UseMockSpesnet
            ? new MockSpesnetTimekeepingClient(_logger)
            : new SpesnetTimekeepingClient(settings, _logger);

    private void PublishResult(SyncResult result)
    {
        var notice = string.IsNullOrWhiteSpace(result.BillingNotice) ? result.Message : result.BillingNotice;
        _form?.ApplySyncProgress(new SyncProgressEventArgs
        {
            Message = result.Success ? notice : result.Message,
            UpdatedWatermark = result.LastSyncedStartTime
        });

        if (!result.Success)
        {
            NotifyIssue(result.Message);
        }
        else
        {
            _configService.ClearSyncProblem();
            NotifyInfo("Sync complete", notice);
        }
    }

    private SyncResult Fail(string message)
    {
        _logger.Error(message);
        return new SyncResult { Success = false, Message = message };
    }

    private bool FormHasUnsavedMappings()
    {
        if (_form is not { IsDisposed: false, IsHandleCreated: true } form)
        {
            return false;
        }

        return form.HasUnsavedMappingChanges;
    }

    private DateTime? TryGetWatermarkUtc()
    {
        if (_form is { IsDisposed: false, IsHandleCreated: true } form)
        {
            return form.GetSyncWatermarkUtc();
        }

        var stored = _configService.LoadSyncState().LastSyncedStartTime;
        return stored is DateTime value ? SouthAfricaClock.ToUtc(value) : null;
    }

    private void CancelToday()
    {
        if (_syncActivity.IsBusy)
        {
            NotifyInfo("Daily sync", "Sync has already started and can no longer be cancelled.");
            return;
        }

        var today = SouthAfricaClock.TodayString();
        var state = _configService.LoadAutoSyncState();
        if (state.CancelledDate == today || state.CompletedDate == today)
        {
            return;
        }

        state.CancelledDate = today;
        _configService.SaveAutoSyncState(state);
        _logger.Info($"Automatic sync cancelled for {today}.");
        NotifyInfo("Daily sync cancelled", "Today's automatic sync was cancelled. You can still sync from the tray menu.");
        UpdateTrayStatus();
    }

    private bool IsAutomaticSyncPending()
    {
        var today = SouthAfricaClock.TodayString();
        var state = _configService.LoadAutoSyncState();
        var syncTime = DailySyncTime.Parse(_configService.LoadSettings().DailySyncTime);
        return SouthAfricaClock.Now().TimeOfDay >= syncTime
            && state.CancelledDate != today
            && state.CompletedDate != today;
    }

    private void MarkCompleted(string today)
    {
        var state = _configService.LoadAutoSyncState();
        state.CompletedDate = today;
        _configService.SaveAutoSyncState(state);
    }

    private void UpdateTrayStatus()
    {
        if (_exitRequested)
        {
            return;
        }

        var nowSa = SouthAfricaClock.Now();
        var today = nowSa.ToString("yyyy-MM-dd");
        var syncAt = DailySyncTime.Parse(_configService.LoadSettings().DailySyncTime);
        var syncTime = DailySyncTime.Format(syncAt);
        var state = _configService.LoadAutoSyncState();
        string text;
        Icon icon;
        var canCancel = state.CancelledDate != today
            && state.CompletedDate != today
            && !_syncActivity.IsBusy;

        if (state.SyncProblem)
        {
            text = string.IsNullOrWhiteSpace(state.SyncProblemMessage)
                ? "Sync problem"
                : state.SyncProblemMessage;
            icon = _problemIcon;
        }
        else if (state.CompletedDate == today)
        {
            text = "Synced today";
            icon = _syncedIcon;
        }
        else if (_syncActivity.IsBusy)
        {
            text = "Not synced yet. Sync in progress";
            icon = _waitingIcon;
        }
        else if (state.CancelledDate == today)
        {
            text = "Not synced. Automatic sync cancelled for today";
            icon = _waitingIcon;
        }
        else if (nowSa.TimeOfDay >= syncAt)
        {
            text = "Not synced yet. Daily sync will run now";
            icon = _waitingIcon;
        }
        else
        {
            text = $"Not synced yet. Daily sync at {syncTime}";
            icon = _waitingIcon;
        }

        _statusItem.Text = Clip(text, 120);
        _cancelItem.Enabled = canCancel;
        _syncNowItem.Enabled = !_syncActivity.IsBusy;
        _tray.Icon = icon;
        _tray.Text = Clip(text, 63);
    }

    private void NotifyIssue(string message)
    {
        _configService.SetSyncProblem(message);
        UpdateTrayStatus();
        ShowNotice(NoticeKind.Issue, "Sync issue", message, ToolTipIcon.Error);
    }

    private void NotifyInfo(string title, string message) =>
        ShowNotice(NoticeKind.None, title, message, ToolTipIcon.Info);

    private void ShowNotice(NoticeKind notice, string title, string message, ToolTipIcon icon)
    {
        if (_exitRequested)
        {
            return;
        }

        _notice = notice;
        _tray.ShowBalloonTip(20_000, Clip(title, 63), Clip(message, 240), icon);
    }

    private void OnBalloonTipClicked()
    {
        switch (_notice)
        {
            case NoticeKind.Issue:
                ShowMainForm();
                break;
        }
    }

    private void NotifyStillRunning()
    {
        if (_toldUserAboutTray)
        {
            return;
        }

        _toldUserAboutTray = true;
        NotifyInfo(
            "Still running",
            "The app stays in the notification area and syncs each day after the time in Settings. Right-click the icon and choose Exit to quit.");
    }

    private void ShowMainForm()
    {
        if (_form is null || _form.IsDisposed)
        {
            _form = new SyncForm(_configService, _logger, _syncActivity);
            _form.FormClosing += OnFormClosing;
        }

        _form.OnSyncIssue = NotifyIssue;
        _form.OnSyncSucceeded = () =>
        {
            _configService.ClearSyncProblem();
            UpdateTrayStatus();
        };

        if (!_form.Visible)
        {
            _form.Show();
        }

        if (_form.WindowState == FormWindowState.Minimized)
        {
            _form.WindowState = FormWindowState.Normal;
        }

        _form.Activate();
    }

    private void OnFormClosing(object? sender, FormClosingEventArgs e)
    {
        if (e.Cancel)
        {
            return;
        }

        if (e.CloseReason == CloseReason.WindowsShutDown)
        {
            _exitRequested = true;
            return;
        }

        if (_exitRequested)
        {
            return;
        }

        e.Cancel = true;
        _form?.Hide();
        NotifyStillRunning();
    }

    private void ExitApplication()
    {
        _exitRequested = true;
        if (_form is { IsDisposed: false })
        {
            _form.Close();
            if (!_form.IsDisposed)
            {
                _exitRequested = false;
                return;
            }
        }

        ExitThread();
    }

    private void Post(Action action)
    {
        if (_ui.IsDisposed)
        {
            return;
        }

        if (_ui.InvokeRequired)
        {
            _ui.BeginInvoke(action);
        }
        else
        {
            action();
        }
    }

    private static string Clip(string value, int max)
    {
        var text = value.ReplaceLineEndings(" ").Trim();
        if (text.Length <= max)
        {
            return text;
        }

        return string.Concat(text.AsSpan(0, max - 3), "...");
    }

    private static Icon CreateIcon(Color background, Action<Graphics> drawGlyph)
    {
        using var bitmap = new Bitmap(32, 32, PixelFormat.Format32bppArgb);
        using (var graphics = Graphics.FromImage(bitmap))
        {
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            graphics.Clear(Color.Transparent);
            using var brush = new SolidBrush(background);
            graphics.FillEllipse(brush, 1, 1, 30, 30);
            drawGlyph(graphics);
        }

        var handle = bitmap.GetHicon();
        try
        {
            using var icon = Icon.FromHandle(handle);
            return (Icon)icon.Clone();
        }
        finally
        {
            DestroyIcon(handle);
        }
    }

    private static void DrawClock(Graphics graphics)
    {
        using var pen = new Pen(Color.White, 2.4f)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round
        };
        graphics.DrawEllipse(pen, 8.5f, 8.5f, 15f, 15f);
        graphics.DrawLine(pen, 16f, 16f, 16f, 11f);
        graphics.DrawLine(pen, 16f, 16f, 21f, 18f);
    }

    private static void DrawCheck(Graphics graphics)
    {
        using var pen = new Pen(Color.White, 3.2f)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round,
            LineJoin = LineJoin.Round
        };
        graphics.DrawLines(pen, [new PointF(8f, 17f), new PointF(14f, 23f), new PointF(24f, 10f)]);
    }

    private static void DrawBang(Graphics graphics)
    {
        using var brush = new SolidBrush(Color.White);
        graphics.FillRectangle(brush, 14, 6, 4, 14);
        graphics.FillEllipse(brush, 13, 22, 6, 6);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _appShutdown.Cancel();
            _timer.Stop();
            _timer.Dispose();
            _showWindowRegistration.Unregister(null);
            _tray.Visible = false;
            _tray.Dispose();
            _menu.Dispose();
            _waitingIcon.Dispose();
            _syncedIcon.Dispose();
            _problemIcon.Dispose();
            _ui.Dispose();
            _appShutdown.Dispose();
        }

        base.Dispose(disposing);
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyIcon(IntPtr handle);

    private enum NoticeKind
    {
        None,
        Issue
    }
}
