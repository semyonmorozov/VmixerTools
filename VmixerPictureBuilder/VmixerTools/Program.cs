using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using PressSharper;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks;

namespace VmixerTools
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            var receipts = ReceiptsParser.ParseReceipts("aromas.xml", "receipts.xml", "images.xml")
                .Where(i => i != null).ToArray();
            
        }

        private static void StatsAgregateTmp(Receipt[] receipts)
        {
            var o = receipts.Select(x => x.Aromas.Count);
            var q = from x in o
                group x by x
                into g
                let count = g.Count()
                orderby count descending
                select new {Value = g.Key, Count = count};
            foreach (var x in q)
            {
                Console.WriteLine("Количество аром: " + x.Value + " Количество рецептов: " + x.Count);
            }
        }

        private static async Task DrawReceipts(IEnumerable<Receipt> receipts)
        {
            foreach (var receipt in receipts)
            {
                await DrawReceipt(receipt).ConfigureAwait(false);
            }
        }

        private static async Task DrawReceipt(Receipt receipt)
        {
            await Task.Run(() =>
            {
                var imgPath = receipt.ImagePath;
                Image receiptImage;
                try
                {
                    receiptImage = Image.FromFile(imgPath);
                }
                catch (FileNotFoundException e)
                {
                    Console.WriteLine($"ReceiptId: {receipt.Id} Неверно указан путь до картинки: {imgPath}");
                    return;
                }
                catch (ArgumentNullException e)
                {
                    Console.WriteLine($"ReceiptId: {receipt.Id} не найдена кртинка для рецепта с таким Id");
                    return;
                }

                var bitmap = new Bitmap(1080, 1080);
                var graphicImage = Graphics.FromImage(bitmap);
                var myBrush = new SolidBrush(Color.White);
                graphicImage.FillRectangle(myBrush, new Rectangle(0, 0, 1080, 1080));
                receiptImage = ResizeWidthWithFixedRatio(receiptImage, 1080);

                try
                {
                    graphicImage.DrawImage(receiptImage,
                        new Point(0, (int) ((1080 - (float) receiptImage.Height) / 2)));
                }
                catch (Exception e)
                {
                    Console.WriteLine($"ReceiptId: {receipt.Id} ImgPath: {imgPath} {e}");
                    return;
                }

                DrawReceiptText(receipt, graphicImage, new Point(100, 20));

                bitmap.Save($"receiptImages/{imgPath.Split('/').Last()}");
            }).ConfigureAwait(false);
        }

        private static void DrawReceiptText(Receipt receipt, Graphics graphicImage, PointF drawingPoint)
        {
            graphicImage.SmoothingMode = SmoothingMode.AntiAlias;

            var font = new Font("Arial", 12, FontStyle.Bold);
            var brush = new SolidBrush(Color.Black);

            var nameStringHeight = DrawString(receipt.Name, graphicImage, font, brush, drawingPoint).Height;
            
            drawingPoint = new PointF(drawingPoint.X,drawingPoint.Y + nameStringHeight);
            var descriptionStringHeight = DrawString(
                receipt.Description, 
                graphicImage, 
                font, 
                brush, 
                new Rectangle((int)drawingPoint.X, (int)drawingPoint.Y, 540, 1080)).Height;
            
            drawingPoint = new PointF(drawingPoint.X,drawingPoint.Y + descriptionStringHeight);
            var pgVgStringHeight = DrawString($"Pg/Vg {receipt.Pg}/{receipt.Vg}", graphicImage, font, brush, drawingPoint).Height;
            
            drawingPoint = new PointF(drawingPoint.X,drawingPoint.Y + pgVgStringHeight);
            var infusionStringHeight = DrawString($"Настаивать {receipt.Infusion} дней", graphicImage, font, brush, drawingPoint).Height;
            
            drawingPoint = new PointF(drawingPoint.X,drawingPoint.Y + infusionStringHeight);

            foreach (var aroma in receipt.Aromas)
            {
                var aromaName = $"{aroma.Key.Name}";
                var aromaCount = $"{aroma.Value} мл.";
                
                var aromaNameStringWidth = DrawString(aromaName, graphicImage, font, brush, drawingPoint).Width;
                drawingPoint = new PointF(drawingPoint.X+aromaNameStringWidth,drawingPoint.Y);
                var aromaStringHeight = DrawString(aromaCount, graphicImage, font, brush, drawingPoint).Height;
                drawingPoint = new PointF(drawingPoint.X-aromaNameStringWidth,drawingPoint.Y+aromaStringHeight);
            }
        }

        private static SizeF DrawString(string str, Graphics graphicImage, Font font, Brush brush, Rectangle layoutRectangle)
        {
            graphicImage.DrawString(str, font, brush, layoutRectangle);
            return graphicImage.MeasureString(str, font,layoutRectangle.Size,StringFormat.GenericDefault);
        }

        private static SizeF DrawString(string str, Graphics graphicImage, Font font, Brush brush, PointF point)
        {
            graphicImage.DrawString(str, font, brush, point);
            return graphicImage.MeasureString(str, font);
        }

        private static Bitmap ResizeWidthWithFixedRatio(Image bitMapImage, int width)
        {
            return ResizeImage(bitMapImage, width, (int) (bitMapImage.Height / (float) bitMapImage.Width * width));
        }

        public static Bitmap ResizeImage(Image image, int width, int height)
        {
            var destRect = new Rectangle(0, 0, width, height);
            var destImage = new Bitmap(width, height);

            destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

            using (var graphics = Graphics.FromImage(destImage))
            {
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                using (var wrapMode = new ImageAttributes())
                {
                    wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                    graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
                }
            }

            return destImage;
        }
    }

    public static class ReceiptsParser
    {
        public static IEnumerable<Receipt> ParseReceipts(string aromasXml, string receiptsXml, string imageLinksXml)
        {
            var aroms = ParseAromas(aromasXml).ToArray();
            var imageLinks = ParseImageLinks(imageLinksXml).ToArray();
            var receipts = ParseReceipts(receiptsXml).ToArray();
            foreach (var receipt in receipts)
            {
                foreach (var aroma in receipt.Aromas)
                {
                    aroma.Key.Name = aroms.FirstOrDefault(b => b.Id == aroma.Key.Id)?.Name;
                }

                receipt.ImagePath = imageLinks.FirstOrDefault(l => l.ReceiptId == receipt.Id)?.Name;
            }

            return receipts;
        }

        private static IEnumerable<ImageLink> ParseImageLinks(string source)
        {
            var document = XDocument.Load(source);
            var blog = new Blog(document);
            return blog.GetImageLinks();
        }

        private static IEnumerable<Aroma> ParseAromas(string source)
        {
            var document = XDocument.Load(source);
            var blog = new Blog(document);
            return blog.GetAromas();
        }

        private static IEnumerable<Receipt> ParseReceipts(string source)
        {
            var document = XDocument.Load(source);
            var blog = new Blog(document);
            var receipts = blog.GetReceipts();
            return receipts.Where(r => r != null);
        }
    }
}