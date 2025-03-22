/**
 * مدیریت آمار اتصال WebRTC
 */
class WebRTCStatsManager {
    constructor(peerConnection, hubConnection, sessionId, updateInterval = 1000) {
        this.peerConnection = peerConnection;
        this.hubConnection = hubConnection;
        this.sessionId = sessionId;
        this.updateInterval = updateInterval;
        this.statsTimer = null;
        this.isMonitoring = false;
        this.lastStats = {
            timestamp: new Date(),
            bytesReceived: 0,
            bytesSent: 0
        };

        // متریک‌های فعلی
        this.currentMetrics = {
            bandwidthUsage: 0,
            latencyMs: 0,
            fps: 0,
            packetLoss: 0,
            resolution: '',
            videoCodec: '',
            audioCodec: '',
            cpuUsage: 0,
            memoryUsageMB: 0
        };

        // ثبت رویداد دریافت آمار از سرور
        this.hubConnection.on("ReceiveConnectionStats", (stats) => {
            this.updateUIWithStats(stats);
        });
    }

    /**
     * شروع مانیتورینگ آمار اتصال
     */
    startMonitoring() {
        if (this.isMonitoring) return;

        this.isMonitoring = true;
        this.collectAndSendStats();

        // راه‌اندازی تایمر برای جمع‌آوری مستمر آمار
        this.statsTimer = setInterval(() => {
            this.collectAndSendStats();
        }, this.updateInterval);

        console.log('WebRTC stats monitoring started');
    }

    /**
     * توقف مانیتورینگ آمار اتصال
     */
    stopMonitoring() {
        if (!this.isMonitoring) return;

        this.isMonitoring = false;
        clearInterval(this.statsTimer);
        this.statsTimer = null;

        console.log('WebRTC stats monitoring stopped');
    }

    /**
     * جمع‌آوری و ارسال آمار اتصال به سرور
     */
    async collectAndSendStats() {
        if (!this.peerConnection || this.peerConnection.connectionState === 'closed') {
            this.stopMonitoring();
            return;
        }

        try {
            // دریافت آمار از RTCPeerConnection
            const stats = await this.peerConnection.getStats();

            // پردازش آمار دریافتی
            this.processStats(stats);

            // ارسال آمار به سرور
            await this.sendStatsToServer();
        } catch (error) {
            console.error('Error collecting WebRTC stats:', error);
        }
    }

    /**
     * پردازش آمار دریافتی از WebRTC
     */
    processStats(stats) {
        const now = new Date();
        let bytesReceived = 0;
        let bytesSent = 0;
        let packetsLost = 0;
        let packetsReceived = 0;
        let currentRtt = 0;

        stats.forEach(report => {
            if (report.type === 'inbound-rtp' && !report.isRemote) {
                // آمار ورودی RTP
                bytesReceived += report.bytesReceived || 0;
                packetsLost += report.packetsLost || 0;
                packetsReceived += report.packetsReceived || 0;

                // محاسبه FPS برای ویدیو
                if (report.kind === 'video' && report.framesDecoded && report.framesPerSecond) {
                    this.currentMetrics.fps = report.framesPerSecond;
                }

                // اطلاعات کدک
                if (report.kind === 'video') {
                    this.currentMetrics.videoCodec = report.codecId;
                } else if (report.kind === 'audio') {
                    this.currentMetrics.audioCodec = report.codecId;
                }
            }
            else if (report.type === 'outbound-rtp' && !report.isRemote) {
                // آمار خروجی RTP
                bytesSent += report.bytesSent || 0;
            }
            else if (report.type === 'track' && report.kind === 'video' && report.frameWidth && report.frameHeight) {
                // وضوح ویدیو
                this.currentMetrics.resolution = `${report.frameWidth}x${report.frameHeight}`;
            }
            else if (report.type === 'candidate-pair' && report.currentRoundTripTime) {
                // زمان تاخیر
                currentRtt = report.currentRoundTripTime * 1000; // تبدیل به میلی‌ثانیه
            }
        });

        // محاسبه پهنای باند مصرفی (کیلوبیت بر ثانیه)
        const timeDiffMs = now - this.lastStats.timestamp;
        if (timeDiffMs > 0) {
            const bytesReceivedDiff = bytesReceived - this.lastStats.bytesReceived;
            const bytesSentDiff = bytesSent - this.lastStats.bytesSent;
            const totalBytesDiff = bytesReceivedDiff + bytesSentDiff;

            // تبدیل بایت بر میلی‌ثانیه به کیلوبیت بر ثانیه
            this.currentMetrics.bandwidthUsage = (totalBytesDiff * 8) / (timeDiffMs / 1000) / 1000;
        }

        // محاسبه درصد از دست رفتن بسته‌ها
        if (packetsReceived > 0) {
            this.currentMetrics.packetLoss = (packetsLost / (packetsReceived + packetsLost)) * 100;
        }

        // تنظیم زمان تاخیر
        this.currentMetrics.latencyMs = currentRtt;

        // ذخیره آمار برای محاسبه بعدی
        this.lastStats = {
            timestamp: now,
            bytesReceived: bytesReceived,
            bytesSent: bytesSent
        };

        // برای اهداف نمایشی، مقادیر CPU و حافظه را شبیه‌سازی می‌کنیم
        // در محیط واقعی، این مقادیر باید از سیستم عامل دریافت شوند
        this.currentMetrics.cpuUsage = Math.random() * 40 + 10; // بین 10% تا 50%
        this.currentMetrics.memoryUsageMB = Math.random() * 500 + 500; // بین 500MB تا 1GB

        // وضعیت اتصال ICE و سیگنالینگ
        this.currentMetrics.iceConnectionState = this.peerConnection.iceConnectionState;
        this.currentMetrics.signalingState = this.peerConnection.signalingState;
    }

    /**
     * ارسال آمار به سرور از طریق SignalR
     */
    async sendStatsToServer() {
        try {
            // ساخت آبجکت آمار برای ارسال
            const statsObject = {
                SessionId: this.sessionId,
                BandwidthUsage: this.currentMetrics.bandwidthUsage.toFixed(2),
                LatencyMs: Math.round(this.currentMetrics.latencyMs),
                FPS: this.currentMetrics.fps.toFixed(1),
                PacketLoss: this.currentMetrics.packetLoss.toFixed(2),
                Resolution: this.currentMetrics.resolution,
                VideoCodec: this.currentMetrics.videoCodec,
                AudioCodec: this.currentMetrics.audioCodec,
                CpuUsage: this.currentMetrics.cpuUsage.toFixed(1),
                MemoryUsageMB: this.currentMetrics.memoryUsageMB.toFixed(0),
                IceConnectionState: this.currentMetrics.iceConnectionState,
                SignalingState: this.currentMetrics.signalingState
            };

            // ارسال به سرور
            await this.hubConnection.invoke("UpdateConnectionStats", this.sessionId, JSON.stringify(statsObject));
        } catch (error) {
            console.error('Error sending stats to server:', error);
        }
    }

    /**
     * به‌روزرسانی رابط کاربری با آمار دریافتی
     */
    updateUIWithStats(stats) {
        // بررسی وجود عناصر UI
        const connectionSpeedElement = document.getElementById('connectionSpeed');
        const latencyElement = document.getElementById('latency');
        const fpsElement = document.getElementById('fps');
        const connectionQualityElement = document.getElementById('connectionQuality');
        const packetLossElement = document.getElementById('packetLoss');
        const resolutionElement = document.getElementById('resolution');
        const cpuUsageElement = document.getElementById('cpuUsage');
        const memoryUsageElement = document.getElementById('memoryUsage');
        const sessionTimeElement = document.getElementById('sessionTime');

        // به‌روزرسانی عناصر در صورت وجود
        if (connectionSpeedElement) {
            connectionSpeedElement.textContent = `${stats.BandwidthUsage} Kbps`;
        }

        if (latencyElement) {
            latencyElement.textContent = `${stats.LatencyMs} ms`;
        }

        if (fpsElement) {
            fpsElement.textContent = `${stats.FPS} FPS`;
        }

        if (connectionQualityElement) {
            let qualityText = 'نامشخص';
            let qualityClass = 'text-muted';

            // تعیین متن و کلاس نمایش کیفیت
            switch (stats.ConnectionQuality) {
                case 1: // Poor
                    qualityText = 'ضعیف';
                    qualityClass = 'text-danger';
                    break;
                case 2: // Fair
                    qualityText = 'متوسط';
                    qualityClass = 'text-warning';
                    break;
                case 3: // Good
                    qualityText = 'خوب';
                    qualityClass = 'text-info';
                    break;
                case 4: // Excellent
                    qualityText = 'عالی';
                    qualityClass = 'text-success';
                    break;
            }

            connectionQualityElement.textContent = qualityText;

            // حذف کلاس‌های قبلی
            connectionQualityElement.classList.remove('text-danger', 'text-warning', 'text-info', 'text-success', 'text-muted');
            // اضافه کردن کلاس جدید
            connectionQualityElement.classList.add(qualityClass);
        }

        if (packetLossElement) {
            packetLossElement.textContent = `${stats.PacketLoss}%`;
        }

        if (resolutionElement) {
            resolutionElement.textContent = stats.Resolution || 'نامشخص';
        }

        if (cpuUsageElement) {
            cpuUsageElement.textContent = `${stats.CpuUsage}%`;
        }

        if (memoryUsageElement) {
            memoryUsageElement.textContent = `${stats.MemoryUsageMB} MB`;
        }

        if (sessionTimeElement) {
            // نمایش زمان به صورت HH:MM:SS
            const connectedTime = this.formatTimeSpan(stats.ConnectedTime);
            sessionTimeElement.textContent = connectedTime;
        }
    }

    /**
     * تبدیل TimeSpan به فرمت HH:MM:SS
     */
    formatTimeSpan(timeSpan) {
        if (!timeSpan) return '00:00:00';

        // تبدیل رشته ISO دریافتی به ساعت، دقیقه و ثانیه
        const matches = timeSpan.match(/PT(?:(\d+)H)?(?:(\d+)M)?(?:(\d+)S)?/);
        if (!matches) return '00:00:00';

        const hours = matches[1] ? parseInt(matches[1]) : 0;
        const minutes = matches[2] ? parseInt(matches[2]) : 0;
        const seconds = matches[3] ? parseInt(matches[3]) : 0;

        return `${hours.toString().padStart(2, '0')}:${minutes.toString().padStart(2, '0')}:${seconds.toString().padStart(2, '0')}`;
    }
}