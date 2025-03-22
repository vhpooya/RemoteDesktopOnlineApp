// مدیریت انتقال فایل از سمت کلاینت

class FileTransferClient {
    constructor(connection, sessionId) {
        this.connection = connection;
        this.sessionId = sessionId;
        this.activeUploads = new Map();
        this.activeDownloads = new Map();
        this.pendingFileChunks = new Map();

        this.setupEventHandlers();
    }

    // تنظیم event handlerها
    setupEventHandlers() {
        // دریافت درخواست شروع آپلود فایل از سرور به سیستم محلی
        this.connection.on("InitiateFileUpload", this.handleInitiateFileUpload.bind(this));

        // دریافت بخشی از فایل در حال آپلود
        this.connection.on("FileUploadChunk", this.handleFileUploadChunk.bind(this));

        // دریافت اطلاعیه تکمیل انتقال
        this.connection.on("FileTransferCompleted", this.handleFileTransferCompleted.bind(this));

        // دریافت اطلاعیه خطا در انتقال
        this.connection.on("FileTransferError", this.handleFileTransferError.bind(this));

        // دریافت اطلاعیه پیشرفت انتقال
        this.connection.on("FileTransferProgress", this.handleTransferProgress.bind(this));

        // دریافت درخواست شروع دانلود فایل از سیستم محلی به سرور
        this.connection.on("InitiateFileDownload", this.handleInitiateFileDownload.bind(this));

        // دریافت درخواست محتوای دایرکتوری
        this.connection.on("RequestDirectoryContents", this.handleRequestDirectoryContents.bind(this));

        // دریافت درخواست لغو انتقال
        this.connection.on("CancelFileTransfer", this.handleCancelFileTransfer.bind(this));
    }

    // پردازش درخواست شروع آپلود فایل
    async handleInitiateFileUpload(data) {
        console.log(`درخواست آپلود فایل دریافت شد: ${data.fileName}`, data);

        try {
            // ایجاد ساختار برای نگهداری اطلاعات فایل در حال آپلود
            const uploadInfo = {
                transferId: data.transferId,
                fileName: data.fileName,
                fileSize: data.fileSize,
                destinationPath: data.destinationPath,
                chunks: [],
                receivedChunks: 0,
                totalChunks: 0,
                status: 'initializing'
            };

            this.activeUploads.set(data.transferId, uploadInfo);

            // ایجاد مسیر مقصد اگر وجود ندارد
            await this.ensureDirectoryExists(data.destinationPath);

            console.log(`آماده دریافت فایل ${data.fileName} در مسیر ${data.destinationPath}`);
        } catch (error) {
            console.error(`خطا در شروع آپلود فایل: ${error.message}`, error);

            // اطلاع خطا به سرور
            await this.connection.invoke("FileTransferError", {
                transferId: data.transferId,
                error: error.message
            });
        }
    }

    // پردازش بخش فایل دریافتی
    async handleFileUploadChunk(data) {
        try {
            const upload = this.activeUploads.get(data.transferId);
            if (!upload) {
                throw new Error(`اطلاعات انتقال با شناسه ${data.transferId} یافت نشد`);
            }

            if (upload.status === 'initializing') {
                upload.status = 'transferring';
                upload.totalChunks = data.totalChunks;

                // ایجاد آرایه برای نگهداری بخش‌های فایل
                upload.chunks = new Array(data.totalChunks);

                // آماده‌سازی برای نوشتن فایل
                upload.fileHandle = await this.openFileForWriting(
                    `${upload.destinationPath}/${upload.fileName}`
                );
            }

            // بررسی لغو شدن انتقال
            if (upload.status === 'cancelled') {
                return;
            }

            // دیکود داده دریافتی
            const chunkData = this.base64ToArrayBuffer(data.chunkData);

            // ذخیره در آرایه بخش‌ها
            upload.chunks[data.chunkIndex] = chunkData;
            upload.receivedChunks++;

            // نوشتن در فایل
            await this.writeChunkToFile(upload.fileHandle, chunkData, data.chunkIndex);

            // بررسی تکمیل انتقال
            if (data.isLastChunk || upload.receivedChunks === upload.totalChunks) {
                upload.status = 'finalizing';

                // بستن فایل
                await this.closeFile(upload.fileHandle);

                console.log(`فایل ${upload.fileName} به صورت کامل دریافت شد`);

                // اطلاع به سرور
                await this.connection.invoke("FileUploadCompleted", {
                    transferId: data.transferId,
                    fileName: upload.fileName,
                    fileSize: upload.fileSize
                });

                // پاکسازی
                this.activeUploads.delete(data.transferId);
            }
        } catch (error) {
            console.error(`خطا در پردازش بخش فایل: ${error.message}`, error);

            // اطلاع خطا به سرور
            await this.connection.invoke("FileTransferError", {
                transferId: data.transferId,
                error: error.message
            });
        }
    }

    // پردازش درخواست دانلود فایل
    async handleInitiateFileDownload(data) {
        console.log(`درخواست دانلود فایل دریافت شد: ${data.filePath}`, data);

        try {
            // بررسی وجود فایل
            const fileExists = await this.checkFileExists(data.filePath);
            if (!fileExists) {
                throw new Error(`فایل ${data.filePath} یافت نشد`);
            }

            // دریافت اطلاعات فایل
            const fileInfo = await this.getFileInfo(data.filePath);

            // ایجاد ساختار برای نگهداری اطلاعات دانلود
            const downloadInfo = {
                transferId: data.transferId,
                callbackId: data.callbackId,
                filePath: data.filePath,
                fileName: fileInfo.name,
                fileSize: fileInfo.size,
                status: 'initializing',
                chunkSize: 64 * 1024, // 64KB
                currentPosition: 0
            };

            this.activeDownloads.set(data.transferId, downloadInfo);

            // شروع ارسال فایل
            downloadInfo.status = 'transferring';
            await this.startFileDownload(downloadInfo);
        } catch (error) {
            console.error(`خطا در شروع دانلود فایل: ${error.message}`, error);

            // اطلاع خطا به سرور
            await this.connection.invoke("FileTransferError", {
                transferId: data.transferId,
                error: error.message
            });
        }
    }

    // شروع ارسال فایل به سرور
    async startFileDownload(downloadInfo) {
        try {
            // باز کردن فایل برای خواندن
            const fileHandle = await this.openFileForReading(downloadInfo.filePath);

            // محاسبه تعداد کل بخش‌ها
            const totalChunks = Math.ceil(downloadInfo.fileSize / downloadInfo.chunkSize);

            // ارسال هر بخش
            for (let i = 0; i < totalChunks; i++) {
                // بررسی لغو شدن انتقال
                if (downloadInfo.status === 'cancelled') {
                    break;
                }

                const position = i * downloadInfo.chunkSize;
                const size = Math.min(downloadInfo.chunkSize, downloadInfo.fileSize - position);

                // خواندن بخش فایل
                const chunkData = await this.readChunkFromFile(fileHandle, position, size);

                // ارسال بخش به سرور
                await this.connection.invoke("ReceiveFileChunk", {
                    callbackId: downloadInfo.callbackId,
                    chunkIndex: i,
                    totalChunks: totalChunks,
                    chunkData: this.arrayBufferToBase64(chunkData),
                    isLastChunk: (i === totalChunks - 1)
                });

                // کمی تأخیر برای جلوگیری از اشباع شبکه
                await new Promise(resolve => setTimeout(resolve, 10));
            }

            // بستن فایل
            await this.closeFile(fileHandle);

            // بروزرسانی وضعیت
            downloadInfo.status = 'completed';

            console.log(`فایل ${downloadInfo.fileName} به صورت کامل ارسال شد`);

            // پاکسازی
            this.activeDownloads.delete(downloadInfo.transferId);
        } catch (error) {
            console.error(`خطا در ارسال فایل: ${error.message}`, error);

            // اطلاع خطا به سرور
            await this.connection.invoke("FileTransferError", {
                transferId: downloadInfo.transferId,
                error: error.message
            });
        }
    }

    // پردازش درخواست محتوای دایرکتوری
    async handleRequestDirectoryContents(data) {
        console.log(`درخواست محتوای پوشه دریافت شد: ${data.path}`, data);

        try {
            // خواندن محتوای دایرکتوری
            const contents = await this.getDirectoryContents(data.path);

            // ارسال پاسخ به سرور
            await this.connection.invoke("DirectoryContentsResponse", {
                requestId: data.requestId,
                contents: contents
            });
        } catch (error) {
            console.error(`خطا در خواندن محتوای پوشه: ${error.message}`, error);

            // ارسال خطا به سرور
            await this.connection.invoke("DirectoryContentsError", {
                requestId: data.requestId,
                error: error.message
            });
        }
    }

    // پردازش درخواست لغو انتقال
    handleCancelFileTransfer(data) {
        console.log(`درخواست لغو انتقال فایل دریافت شد: ${data.transferId}`);

        // بررسی وجود در لیست آپلودها
        if (this.activeUploads.has(data.transferId)) {
            const upload = this.activeUploads.get(data.transferId);
            upload.status = 'cancelled';

            // بستن فایل در صورت باز بودن
            if (upload.fileHandle) {
                this.closeFile(upload.fileHandle)
                    .catch(error => console.error(`خطا در بستن فایل: ${error.message}`));
            }

            this.activeUploads.delete(data.transferId);
        }

        // بررسی وجود در لیست دانلودها
        if (this.activeDownloads.has(data.transferId)) {
            const download = this.activeDownloads.get(data.transferId);
            download.status = 'cancelled';
            this.activeDownloads.delete(data.transferId);
        }
    }

    // پردازش اطلاعیه تکمیل انتقال
    handleFileTransferCompleted(data) {
        console.log(`انتقال فایل با شناسه ${data.transferId} تکمیل شد`);

        // پاکسازی منابع مرتبط با این انتقال
        this.activeUploads.delete(data.transferId);
        this.activeDownloads.delete(data.transferId);
    }

    // پردازش اطلاعیه خطا در انتقال
    handleFileTransferError(data) {
        console.error(`خطا در انتقال فایل با شناسه ${data.transferId}: ${data.error}`);

        // پاکسازی منابع مرتبط با این انتقال
        this.activeUploads.delete(data.transferId);
        this.activeDownloads.delete(data.transferId);
    }

    // پردازش اطلاعیه پیشرفت انتقال
    handleTransferProgress(data) {
        // ارسال به UI برای نمایش پیشرفت
        console.log(`پیشرفت انتقال ${data.transferId}: ${data.progress}%`);

        // فراخوانی callback مناسب برای بروزرسانی UI
        if (typeof window.updateFileTransferProgress === 'function') {
            window.updateFileTransferProgress(data);
        }
    }

    // توابع کمکی برای کار با فایل‌ها

    // تبدیل Base64 به ArrayBuffer
    base64ToArrayBuffer(base64) {
        const binaryString = window.atob(base64);
        const len = binaryString.length;
        const bytes = new Uint8Array(len);
        for (let i = 0; i < len; i++) {
            bytes[i] = binaryString.charCodeAt(i);
        }
        return bytes.buffer;
    }

    // تبدیل ArrayBuffer به Base64
    arrayBufferToBase64(buffer) {
        let binary = '';
        const bytes = new Uint8Array(buffer);
        const len = bytes.byteLength;
        for (let i = 0; i < len; i++) {
            binary += String.fromCharCode(bytes[i]);
        }
        return window.btoa(binary);
    }

    // ایجاد مسیر اگر وجود ندارد
    async ensureDirectoryExists(path) {
        // در مرورگر، این تابع باید با file system API سیستم عامل کار کند
        // این پیاده‌سازی فقط یک نمونه است و به محیط اجرا بستگی دارد
        console.log(`اطمینان از وجود مسیر: ${path}`);
        return true;
    }

    // باز کردن فایل برای نوشتن
    async openFileForWriting(filePath) {
        // در مرورگر، این تابع باید با file system API سیستم عامل کار کند
        // این پیاده‌سازی فقط یک نمونه است و به محیط اجرا بستگی دارد
        console.log(`باز کردن فایل برای نوشتن: ${filePath}`);
        return { path: filePath };
    }

    // نوشتن بخش داده در فایل
    async writeChunkToFile(fileHandle, chunkData, position) {
        // در مرورگر، این تابع باید با file system API سیستم عامل کار کند
        // این پیاده‌سازی فقط یک نمونه است و به محیط اجرا بستگی دارد
        console.log(`نوشتن بخش ${position} در فایل: ${fileHandle.path}`);
        return true;
    }

    // بستن فایل
    async closeFile(fileHandle) {
        // در مرورگر، این تابع باید با file system API سیستم عامل کار کند
        // این پیاده‌سازی فقط یک نمونه است و به محیط اجرا بستگی دارد
        console.log(`بستن فایل: ${fileHandle.path}`);
        return true;
    }

    // بررسی وجود فایل
    async checkFileExists(filePath) {
        // در مرورگر، این تابع باید با file system API سیستم عامل کار کند
        // این پیاده‌سازی فقط یک نمونه است و به محیط اجرا بستگی دارد
        console.log(`بررسی وجود فایل: ${filePath}`);
        return true;
    }

    // دریافت اطلاعات فایل
    async getFileInfo(filePath) {
        // در مرورگر، این تابع باید با file system API سیستم عامل کار کند
        // این پیاده‌سازی فقط یک نمونه است و به محیط اجرا بستگی دارد
        console.log(`دریافت اطلاعات فایل: ${filePath}`);
        return {
            name: filePath.split('/').pop(),
            size: 1024 * 1024 // 1MB (مقدار نمونه)
        };
    }

    // باز کردن فایل برای خواندن
    async openFileForReading(filePath) {
        // در مرورگر، این تابع باید با file system API سیستم عامل کار کند
        // این پیاده‌سازی فقط یک نمونه است و به محیط اجرا بستگی دارد
        console.log(`باز کردن فایل برای خواندن: ${filePath}`);
        return { path: filePath };
    }

    // خواندن بخش داده از فایل
    async readChunkFromFile(fileHandle, position, size) {
        // در مرورگر، این تابع باید با file system API سیستم عامل کار کند
        // این پیاده‌سازی فقط یک نمونه است و به محیط اجرا بستگی دارد
        console.log(`خواندن بخش از فایل ${fileHandle.path} از موقعیت ${position} به اندازه ${size}`);
        return new ArrayBuffer(size);
    }

    // خواندن محتوای دایرکتوری
    async getDirectoryContents(dirPath) {
        // در مرورگر، این تابع باید با file system API سیستم عامل کار کند
        // این پیاده‌سازی فقط یک نمونه است و به محیط اجرا بستگی دارد
        console.log(`خواندن محتوای دایرکتوری: ${dirPath}`);
        return [
            {
                name: 'sample-file.txt',
                fullName: `${dirPath}/sample-file.txt`,
                size: 1024,
                lastWriteTime: new Date().toISOString(),
                isDirectory: false
            },
            {
                name: 'sample-folder',
                fullName: `${dirPath}/sample-folder`,
                lastWriteTime: new Date().toISOString(),
                isDirectory: true
            }
        ];
    }
}

// ایجاد نمونه از کلاس و اتصال به کانکشن SignalR
if (typeof window !== 'undefined') {
    window.initFileTransferClient = function (connection, sessionId) {
        window.fileTransferClient = new FileTransferClient(connection, sessionId);
        console.log('سیستم انتقال فایل راه‌اندازی شد');
        return window.fileTransferClient;
    };
}