using System.IO;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using yEnc;

// ReSharper disable ReturnValueOfPureMethodIsNotUsed

namespace yEncTests
{
    [TestClass]
    [DeploymentItem("data", "data")]
    public class YEncTests
    {
        [TestMethod]
        public async Task ShouldReadSinglePartFile()
        {
            byte[] expected = File.ReadAllBytes(@"data\singlepart\testfile.txt");
            using (FileStream stream = File.OpenRead(@"data\singlepart\00000005.ntx"))
            {
                YEncDecodedPart actual = await YEnc.DecodePart(stream);

                Assert.IsFalse(actual.IsFilePart);
                CollectionAssert.AreEqual(expected, actual.Data);
            }
        }

        [TestMethod]
        public void ShouldReadSinglePartFileFromStringList()
        {
            byte[] expected = File.ReadAllBytes(@"data\singlepart\testfile.txt");
            string[] encodedLines = File.ReadAllLines(@"data\singlepart\00000005.ntx", YEnc.DefaultEncoding);
            YEncDecodedPart actual = YEnc.DecodePart(encodedLines);
            Assert.IsFalse(actual.IsFilePart);
            CollectionAssert.AreEqual(expected, actual.Data);
        }

        [TestMethod]
        public async Task ShouldReadFilePart()
        {
            const int expectedDataLength = 11250;
            using (FileStream stream = File.OpenRead(@"data\multipart\00000020.ntx"))
            {
                YEncDecodedPart actual = await YEnc.DecodePart(stream);

                Assert.IsTrue(actual.IsFilePart);
                Assert.AreEqual(expectedDataLength, actual.Data.Length);
            }
        }

        [TestMethod]
        public void ShouldReadFilePartFromStringList()
        {
            const int expectedDataLength = 11250;
            string[] encodedLines = File.ReadAllLines(@"data\multipart\00000020.ntx", YEnc.DefaultEncoding);
            YEncDecodedPart actual = YEnc.DecodePart(encodedLines);
            Assert.IsTrue(actual.IsFilePart);
            Assert.AreEqual(expectedDataLength, actual.Data.Length);
        }

        [TestMethod]
        public async Task ShouldReadMultiPartFile()
        {
            const string expectedFileName = "joystick.jpg";
            byte[] expected = File.ReadAllBytes(@"data\multipart\joystick.jpg");
            using (FileStream stream1 = File.OpenRead(@"data\multipart\00000020.ntx"))
            using (FileStream stream2 = File.OpenRead(@"data\multipart\00000021.ntx"))
            using (var actual = new MemoryStream())
            {
                YEncDecodedStream stream = await YEnc.Decode(new[] { stream1, stream2 });
                string actualFileName = stream.FileName;
                stream.CopyTo(actual);

                Assert.AreEqual(expectedFileName, actualFileName);
                CollectionAssert.AreEqual(expected, actual.ToArray());
            }
        }

        [TestMethod]
        public async Task ShouldReadMultiPartFileWhenPartsAreOutOfOrder()
        {
            const string expectedFileName = "joystick.jpg";
            byte[] expected = File.ReadAllBytes(@"data\multipart\joystick.jpg");
            using (FileStream stream1 = File.OpenRead(@"data\multipart\00000020.ntx"))
            using (FileStream stream2 = File.OpenRead(@"data\multipart\00000021.ntx"))
            using (var actual = new MemoryStream())
            {
                YEncDecodedStream stream = await YEnc.Decode(new[] { stream2, stream1 });
                string actualFileName = stream.FileName;
                stream.CopyTo(actual);

                Assert.AreEqual(expectedFileName, actualFileName);
                CollectionAssert.AreEqual(expected, actual.ToArray());
            }
        }

        [TestMethod]
        public async Task ShouldThrowWhenMultiPartAndThenSinglePart()
        {
            const string expectedMessage = "Unexpected single-part file.";
            using (FileStream stream1 = File.OpenRead(@"data\multipart\00000020.ntx"))
            using (FileStream stream2 = File.OpenRead(@"data\singlepart\00000005.ntx"))
            {
                try
                {
                    await YEnc.Decode(new[] {stream1, stream2});
                }
                catch (YEncException ex)
                {
                    Assert.AreEqual(expectedMessage, ex.Message);
                }
            }
        }

        [TestMethod]
        public async Task ShouldThrowWhenSinglePartAndThenMultiPart()
        {
            const string expectedMessage = "Unexpected file part.";
            using (FileStream stream1 = File.OpenRead(@"data\multipart\00000020.ntx"))
            using (FileStream stream2 = File.OpenRead(@"data\singlepart\00000005.ntx"))
            {
                try
                {
                    await YEnc.Decode(new[] { stream2, stream1 });
                }
                catch (YEncException ex)
                {
                    Assert.AreEqual(expectedMessage, ex.Message);
                }
            }
        }

        [TestMethod]
        public async Task ShouldThrowWhenMultipleSinglePartFiles()
        {
            const string expectedMessage = "Unexpected second single-part file.";
            using (FileStream stream1 = File.OpenRead(@"data\singlepart\00000005.ntx"))
            using (FileStream stream2 = File.OpenRead(@"data\singlepart\00000005.ntx"))
            {
                try
                {
                    await YEnc.Decode(new[] { stream2, stream1 });
                }
                catch (YEncException ex)
                {
                    Assert.AreEqual(expectedMessage, ex.Message);
                }
            }
        }
    }
}
