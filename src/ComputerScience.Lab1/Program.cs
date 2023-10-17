using System.Drawing;
using ComputerScience.Lab1.Configuration;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using MatFileHandler;
using Microsoft.Extensions.Configuration;

namespace ComputerScience.Lab1
{
    public static class Program
    {
        public static void Main()
        {
            var config = GetConfiguration();
            var template = config.TemplateArea;
            
            if (!Directory.Exists(config.ImageSaveLocation))
            {
                Directory.CreateDirectory(config.ImageSaveLocation);
            }

            Console.WriteLine($"Processing MAT project file \"{Path.GetFileName(config.MatFileLocation)}\"");
            Console.WriteLine($"Using \"{config.Image1VariableName}\" variable as source image");
            Console.WriteLine($"Searching match in \"{config.Image2VariableName}\" variable");
            Console.WriteLine($"Template location:      ({template.X}, {template.Y})");
            Console.WriteLine($"Template size:          ({template.Width}, {template.Height})");
            Console.WriteLine($"Fitness threshold:      {config.CcoeffThreshold}");
            
            var matFile = ReadMat(config.MatFileLocation);

            var image1Source = matFile[config.Image1VariableName].Value.ConvertTo2dDoubleArray()!;
            var image2Source = matFile[config.Image2VariableName].Value.ConvertTo2dDoubleArray()!;

            using var image1GreyScaled = GrayScale(image1Source);
            using var image2GreyScaled = GrayScale(image2Source);
            using var templateImage = Crop(image1GreyScaled, new Rectangle(template.X, template.Y, template.Width, template.Height));
            
            var bestMatch = GetMatches
            (
                image2GreyScaled,
                templateImage,
                config.CcoeffThreshold,
                TemplateMatchingType.CcoeffNormed
            ).OrderByDescending(x => x.Fitness).FirstOrDefault();

            if (bestMatch == default)
            {
                Console.WriteLine("No matches found. Aborting");
                Console.Read();
                return;
            }

            using var bestMatchImage = Crop(image2GreyScaled, new Rectangle(bestMatch.X, bestMatch.Y, template.Width, template.Height));
            
            image1GreyScaled.Save(Path.Combine(config.ImageSaveLocation, "image1GreyScaled.bmp"));
            image2GreyScaled.Save(Path.Combine(config.ImageSaveLocation, "image2GreyScaled.bmp"));
            templateImage.Save(Path.Combine(config.ImageSaveLocation, "templateImage.bmp"));
            bestMatchImage.Save(Path.Combine(config.ImageSaveLocation, "bestMatchImage.bmp"));
            
            Console.WriteLine($"Best match fitness:     {bestMatch.Fitness}");
            Console.WriteLine($"Best match location:    ({template.X}, {template.Y})");
            Console.WriteLine($"Shift:                  ({template.X - bestMatch.X}, {template.Y - bestMatch.Y})");
            Console.Read();
        }

        private static AppConfiguration GetConfiguration()
        {
            var config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false)
                .Build();
            
            return config.Get<AppConfiguration>() ?? throw new InvalidOperationException("Missing configuration");
        }

        private static IMatFile ReadMat(string matName)
        {
            IMatFile matFile;
            using (var fileStream = new FileStream(matName, FileMode.Open))
            {
                var reader = new MatFileReader(fileStream);
                matFile = reader.Read();
            }

            return matFile;
        }

        private static Image<Gray, byte> GrayScale(double[,] source)
        {
            var rowsCount = source.GetLength(0);
            var columnsCount = source.GetLength(1);
            
            var max = GetMax(source);

            var image = new Image<Gray, byte>(columnsCount, rowsCount);

            for (var i = 0; i < rowsCount; i++)
            {
                for (var j = 0; j < columnsCount; j++)
                {
                    image[i, j] = new Gray(source[i, j] * 255 / max);
                }
            }
            
            return image;
        }   
        
        private static double GetMax(double[,] source)
        {
            var max = double.MinValue;
            
            var rowsCount = source.GetLength(0);
            var columnsCount = source.GetLength(1);

            for (var i = 0; i < rowsCount; i++)
            {
                for (var j = 0; j < columnsCount; j++)
                {
                    if (source[i, j] > max)
                    {
                        max = source[i, j];
                    }
                }
            }

            return max;
        }   
        
        private static Image<Gray, byte> Crop(CvArray<byte> source, Rectangle rectangle)
        {
            var result = new Image<Gray, byte>(rectangle.Size);

            var srcRoi = new Mat(source.Mat, rectangle);
            var dstRoi = new Mat(result.Mat, rectangle with { X = 0, Y = 0 });

            srcRoi.CopyTo(dstRoi);

            return result;
        }    
        
        private static IEnumerable<(int X, int Y, float Fitness)> GetMatches(Image<Gray, byte> image, Image<Gray, byte> template, float threshold, TemplateMatchingType matchingType)
        {
            using var matches = image.MatchTemplate(template, matchingType);
            
            var rowsCount = matches.Data.GetLength(0);
            var columnsCount = matches.Data.GetLength(1);
            
            for (var i = 0; i < rowsCount; i++)
            {
                for (var j = 0; j < columnsCount; j++)
                {
                    var fitness = matches.Data[i, j, 0];
                    if (fitness >= threshold)
                    {
                        yield return (j, i, fitness);
                    }
                }
            }
        }
    }
}
