using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace TestConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Enter url. Or just return to quit.");

            var uri = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(uri))
            {
                return;
            }

            Console.WriteLine("Enter path to jpg to be uploaded. Or just return to quit.");

            var tester = new Tester();

            while (true)
            {
                var filePath = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(filePath))
                {
                    return;
                }

                if (!System.IO.File.Exists(filePath))
                {
                    Console.WriteLine("File was not found.");
                }
                else
                {
                    var task = tester.Call(uri, filePath);
                    task.Wait();
                }
            }
        }

        public class Tester
        {
            public async Task Call(string uri, string imageFilePath)
            {
                byte[] byteData = GetImageAsByteArray(imageFilePath);
                HttpClient client = new HttpClient();

                using (ByteArrayContent content = new ByteArrayContent(byteData))
                {
                    content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                    var response = await client.PostAsync(uri, content);
                    string contentString = await response.Content.ReadAsStringAsync();

                    Console.WriteLine("\nResponse:\n");
                    Console.WriteLine(contentString);
                }
            }
        }

        /// <summary>
        /// Returns the contents of the specified file as a byte array.
        /// </summary>
        /// <param name="imageFilePath">The image file to read.</param>
        /// <returns>The byte array of the image data.</returns>
        static byte[] GetImageAsByteArray(string imageFilePath)
        {
            FileStream fileStream = new FileStream(imageFilePath, FileMode.Open, FileAccess.Read);
            BinaryReader binaryReader = new BinaryReader(fileStream);
            return binaryReader.ReadBytes((int)fileStream.Length);
        }
    }
}
