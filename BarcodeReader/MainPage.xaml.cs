using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Imaging;
using Windows.Media.Capture;
using Windows.Media.Devices;
using Windows.Media.MediaProperties;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// Die Elementvorlage "Leere Seite" ist unter http://go.microsoft.com/fwlink/?LinkId=391641 dokumentiert.

namespace BarcodeReader
{
    /// <summary>
    /// Eine leere Seite, die eigenständig verwendet werden kann oder auf die innerhalb eines Rahmens navigiert werden kann.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        MediaCapture photoTaker;       
        private DispatcherTimer dispatcherTimer;

        public MainPage()
        {
            this.InitializeComponent();          
            this.photoTaker = (App.Current as App).MediaCapture;
            this.NavigationCacheMode = NavigationCacheMode.Required;
        }

        public void DispatcherTimerSetup()
        {
            dispatcherTimer = new DispatcherTimer();
            dispatcherTimer.Tick += dispatcherTimer_Tick;
            dispatcherTimer.Interval = new TimeSpan(0, 0, 1);
            dispatcherTimer.Start();
        }

        private async void dispatcherTimer_Tick(object sender, object e)
        {
            try
            {
                ImageEncodingProperties imgFormat = ImageEncodingProperties.CreateJpeg();

                // a file to save a photo
                StorageFile file = await ApplicationData.Current.LocalFolder.CreateFileAsync("Photo.jpeg", CreationCollisionOption.ReplaceExisting);
                await this.photoTaker.CapturePhotoToStorageFileAsync(imgFormat, file);
                if (file != null)
                {              
                    BarcodeProxy proxy = new BarcodeProxy();
                    proxy.BarcodeScanCompleted += proxy_BarcodeScanCompleted;
                    await proxy.ScanBarcodeAsync(file);
                }
            }
            catch (Exception)
            {
                (Application.Current as App).CleanupCaptureResources();
            }
        }

       

        /// <summary>
        /// Wird aufgerufen, wenn diese Seite in einem Rahmen angezeigt werden soll.
        /// </summary>
        /// <param name="e">Ereignisdaten, die beschreiben, wie diese Seite erreicht wurde.
        /// Dieser Parameter wird normalerweise zum Konfigurieren der Seite verwendet.</param>
        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            await this.InitializeCameraAsync();
            this.PhotoPreview.Source = this.photoTaker;
            await this.photoTaker.StartPreviewAsync();
            this.DispatcherTimerSetup();
        }                 

        private async void proxy_BarcodeScanCompleted(object sender, BarcodeScanCompletedEventArgs e)
        {
            await this.photoTaker.StopPreviewAsync();
            
        }

        private async Task<StorageFile> TestWithImage()
        {
            //Test File
            StorageFile file;
            Uri uri = new System.Uri("ms-appx:///Assets/TestBarcode.JPG");
            file = await Windows.Storage.StorageFile.GetFileFromApplicationUriAsync(uri);
            BitmapImage bitmapImage = new BitmapImage();
            using (IRandomAccessStream fileStream = await file.OpenAsync(FileAccessMode.Read))
            {
                bitmapImage.SetSource(fileStream);
            }

            this.TestImage.Source = bitmapImage;

            return file;
        }


        private void Reset_Click(object sender, RoutedEventArgs e)
        {
            this.TestImage.Source = null;
        }


        private async Task InitializeCameraAsync()
        {
            var cameraID = await GetCameraID(Windows.Devices.Enumeration.Panel.Back);
            
            MediaCaptureInitializationSettings settings = new MediaCaptureInitializationSettings();
            settings.PhotoCaptureSource = Windows.Media.Capture.PhotoCaptureSource.VideoPreview;
            settings.StreamingCaptureMode = Windows.Media.Capture.StreamingCaptureMode.Video;
            settings.AudioDeviceId = string.Empty;
            settings.VideoDeviceId = cameraID.Id;
            await this.photoTaker.InitializeAsync(settings);

            var maxResolution = this.photoTaker.VideoDeviceController.GetAvailableMediaStreamProperties(MediaStreamType.Photo).Aggregate((i1, i2) => (i1 as VideoEncodingProperties).Width > (i2 as VideoEncodingProperties).Width ? i1 : i2);
            await this.photoTaker.VideoDeviceController.SetMediaStreamPropertiesAsync(MediaStreamType.Photo, maxResolution);
            FocusSettings focusSetting = new FocusSettings() { Mode = FocusMode.Continuous, AutoFocusRange = AutoFocusRange.Normal, DisableDriverFallback = false, WaitForFocus = true };
            this.photoTaker.VideoDeviceController.FocusControl.Configure(focusSetting);
        }

        private static async Task<DeviceInformation> GetCameraID(Windows.Devices.Enumeration.Panel desired)
        {
            DeviceInformation deviceID = null;
            deviceID = (await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture)).FirstOrDefault(x => x.EnclosureLocation != null && x.EnclosureLocation.Panel == desired);
            if (deviceID == null)
            {
                MessageDialog dlg = new MessageDialog(string.Format("Camera of type {0} doesn't exist, try to access first recognized cammera.", desired));
                await dlg.ShowAsync();
                //Try fallback to first recognized
                deviceID = (await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture)).FirstOrDefault();
            }

            return deviceID;
        }


        private async void CapturePhoto_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ImageEncodingProperties imgFormat = ImageEncodingProperties.CreateJpeg();

                // a file to save a photo
                StorageFile file = await ApplicationData.Current.LocalFolder.CreateFileAsync("Photo.jpeg", CreationCollisionOption.ReplaceExisting);
                await this.photoTaker.CapturePhotoToStorageFileAsync(imgFormat, file);
                if (file != null)
                {
                    BarcodeProxy proxy = new BarcodeProxy();
                    proxy.BarcodeScanCompleted += proxy_BarcodeScanCompleted;
                    await proxy.ScanBarcodeAsync(file);
                }
            }
            catch (Exception)
            {
                (Application.Current as App).CleanupCaptureResources();
            }
        }
    }
}
