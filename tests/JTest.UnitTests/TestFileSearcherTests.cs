using JTest.Core.Utilities;
using System.Data;

namespace JTest.UnitTests
{    
    public class TestFileSearcherTests
    {   
        [Fact]
        public void When_Pattern_Includes_AllSubDirectories_Then_Returns_AllJsonFiles()
        {
            // Arrange
            const string pattern = "TestFiles/TestSuites/**/*";
            string[] expectedJsonFileNames =
            [
                "test-file.json",
                "test-file-with-categories.json",
                "nested-test-file.json"
            ];

            // Act
            var result = JsonFileSearcher.Search([pattern]);

            // Assert
            var resultFileNames = result.Select(Path.GetFileName);
            Assert.Equal(3, result.Length);
            Assert.All(resultFileNames, resultFileName =>
            {                
                Assert.Contains(resultFileName, expectedJsonFileNames);
            });
        }

        [Fact]
        public void When_Pattern_HasExcludePattern_Then_Returns_Only_IncludedMatches()
        {
            // Arrange
            string[] patterns =
            [
                "TestFiles/TestSuites/*",
                "!TestFiles/TestSuites/*-with-categories.json"
            ];
            const string expectedJsonFileName = "test-file.json";

            // Act
            var result = JsonFileSearcher.Search(patterns);

            // Assert
            var resultFileNames = result
                .Select(Path.GetFileName)
                .ToArray();

            Assert.Single(result);
            Assert.Contains(expectedJsonFileName, resultFileNames);
        }

        [Fact]
        public void Only_Returns_Json_Files()
        {
            // Arrange
            const string pattern = "TestFiles/TestSuites/*";            
            const string expectedFileNotReturned = "TestFile.txt";

            // Act
            var result = JsonFileSearcher.Search([pattern]);

            // Assert
            var resultFileNames = result.Select(Path.GetFileName);                        
            Assert.DoesNotContain(expectedFileNotReturned, resultFileNames);
        }

        [Fact]
        public void When_Search_Then_MaintainsPatternArgumentOrder()
        {
            // Arrange
            const string pattern1 = "TestFiles/TestSuites/test-file.json";
            const string pattern2 = "TestFiles/TestSuites/test-file-with-categories.json";            

            // Act
            var result = JsonFileSearcher.Search([pattern1, pattern2]);

            // Assert
            var resultFileNames = result.Select(Path.GetFileName);
            Assert.Equal(
               Path.GetFileName(pattern1),
               resultFileNames.First()
            );
            Assert.Equal(
               Path.GetFileName(pattern2),
               resultFileNames.Last()
            );
        }

        [Fact]
        public void When_Specifying_Categories_Then_Returns_Only_TestFiles_With_Categories()
        {
            // Arrange
            const string pattern = "TestFiles/TestSuites/*";
            const string expectedFileNotReturned = "test-file.json";
            const string expectedFileReturned = "test-file-with-categories.json";
            const string category = "testCategory1";

            // Act
            var result = JsonFileSearcher.Search([pattern], [category]);

            // Assert
            var resultFileNames = result.Select(Path.GetFileName);
            Assert.Single(result);
            Assert.Contains(expectedFileReturned, resultFileNames);
            Assert.DoesNotContain(expectedFileNotReturned, resultFileNames);
        }
    }
}
