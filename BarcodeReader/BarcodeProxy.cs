using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Popups;
using Windows.UI.Xaml.Media.Imaging;

namespace BarcodeReader
{
    public class BarcodeProxy
    {
        #region Fields

        private object locker = new object();

        #endregion

        #region Events

        public event EventHandler<BarcodeScanCompletedEventArgs> BarcodeScanCompleted;

        protected virtual void OnBarcodeScannCompleted(BarcodeScanCompletedEventArgs e)
        {
            EventHandler<BarcodeScanCompletedEventArgs> handler = BarcodeScanCompleted;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        #endregion

        #region Methods

        public async Task ScanBarcodeAsync(Windows.Storage.StorageFile file)
        {
            WriteableBitmap bitmap;
            BitmapDecoder decoder;
            using (IRandomAccessStream str = await file.OpenReadAsync())
            {
                decoder = await BitmapDecoder.CreateAsync(str);
                bitmap = new WriteableBitmap(Convert.ToInt32(decoder.PixelWidth), Convert.ToInt32(decoder.PixelHeight));
                await bitmap.SetSourceAsync(str);
            }

            lock (locker)
            {                
                ZXing.BarcodeReader reader = new ZXing.BarcodeReader();
                reader.Options.PossibleFormats = new ZXing.BarcodeFormat[] { ZXing.BarcodeFormat.CODE_128, ZXing.BarcodeFormat.QR_CODE, ZXing.BarcodeFormat.CODE_39 };
                reader.Options.TryHarder = true;
                reader.AutoRotate = true;
                var results = reader.Decode(bitmap);
                if (results == null)
                {
                    this.OnBarcodeScannCompleted(new BarcodeScanCompletedEventArgs(false, string.Empty));
                }
                else
                {
                    this.OnBarcodeScannCompleted(new BarcodeScanCompletedEventArgs(true, results.Text));
                }
            }
        }

        #endregion
    }

    public class BarcodeScanCompletedEventArgs
        : EventArgs
    {
        private string barcode;
        private bool barcodeFound;

        public BarcodeScanCompletedEventArgs(bool found, string code)
        {
            this.barcodeFound = found;
            this.barcode = code;
        }

        public bool BarcodeFound
        {
            get { return barcodeFound; }
            set { barcodeFound = value; }
        }

        public string Barcode
        {
            get { return barcode; }
            set { barcode = value; }
        }
    }

}
