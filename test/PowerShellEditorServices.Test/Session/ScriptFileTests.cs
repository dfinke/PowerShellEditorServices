﻿//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.PowerShell.EditorServices;
using Microsoft.PowerShell.EditorServices.Test.Shared;
using System;
using System.IO;
using System.Linq;
using Xunit;

namespace PSLanguageService.Test
{
    public class ScriptFileChangeTests
    {

#if CoreCLR
        private static readonly Version PowerShellVersion = new Version(6, 1);
#else
        private static readonly Version PowerShellVersion = new Version(5, 1);
#endif

        [Fact]
        public void CanApplySingleLineInsert()
        {
            this.AssertFileChange(
                "This is a test.",
                "This is a working test.",
                new FileChange
                {
                    Line = 1,
                    EndLine = 1,
                    Offset = 10,
                    EndOffset = 10,
                    InsertString = " working"
                });
        }

        [Fact]
        public void CanApplySingleLineReplace()
        {
            this.AssertFileChange(
                "This is a potentially broken test.",
                "This is a working test.",
                new FileChange
                {
                    Line = 1,
                    EndLine = 1,
                    Offset = 11,
                    EndOffset = 29,
                    InsertString = "working"
                });
        }

        [Fact]
        public void CanApplySingleLineDelete()
        {
            this.AssertFileChange(
                "This is a test of the emergency broadcasting system.",
                "This is a test.",
                new FileChange
                {
                    Line = 1,
                    EndLine = 1,
                    Offset = 15,
                    EndOffset = 52,
                    InsertString = ""
                });
        }

        [Fact]
        public void CanApplyMultiLineInsert()
        {
            this.AssertFileChange(
                TestUtilities.NormalizeNewlines("first\nsecond\nfifth"),
                TestUtilities.NormalizeNewlines("first\nsecond\nthird\nfourth\nfifth"),
                new FileChange
                {
                    Line = 3,
                    EndLine = 3,
                    Offset = 1,
                    EndOffset = 1,
                    InsertString = TestUtilities.NormalizeNewlines("third\nfourth\n")
                });
        }

        [Fact]
        public void CanApplyMultiLineReplace()
        {
            this.AssertFileChange(
                TestUtilities.NormalizeNewlines("first\nsecoXX\nXXfth"),
                TestUtilities.NormalizeNewlines("first\nsecond\nthird\nfourth\nfifth"),
                new FileChange
                {
                    Line = 2,
                    EndLine = 3,
                    Offset = 5,
                    EndOffset = 3,
                    InsertString = TestUtilities.NormalizeNewlines("nd\nthird\nfourth\nfi")
                });
        }

        [Fact]
        public void CanApplyMultiLineReplaceWithRemovedLines()
        {
            this.AssertFileChange(
                TestUtilities.NormalizeNewlines("first\nsecoXX\nREMOVE\nTHESE\nLINES\nXXfth"),
                TestUtilities.NormalizeNewlines("first\nsecond\nthird\nfourth\nfifth"),
                new FileChange
                {
                    Line = 2,
                    EndLine = 6,
                    Offset = 5,
                    EndOffset = 3,
                    InsertString = TestUtilities.NormalizeNewlines("nd\nthird\nfourth\nfi")
                });
        }

        [Fact]
        public void CanApplyMultiLineDelete()
        {
            this.AssertFileChange(
                TestUtilities.NormalizeNewlines("first\nsecond\nREMOVE\nTHESE\nLINES\nthird"),
                TestUtilities.NormalizeNewlines("first\nsecond\nthird"),
                new FileChange
                {
                    Line = 3,
                    EndLine = 6,
                    Offset = 1,
                    EndOffset = 1,
                    InsertString = ""
                });
        }

        [Fact]
        public void CanApplyEditsToEndOfFile()
        {
            this.AssertFileChange(
                TestUtilities.NormalizeNewlines("line1\nline2\nline3\n\n"),
                TestUtilities.NormalizeNewlines("line1\nline2\nline3\n\n\n\n"),
                new FileChange
                {
                    Line = 5,
                    EndLine = 5,
                    Offset = 1,
                    EndOffset = 1,
                    InsertString = Environment.NewLine + Environment.NewLine
                });
        }

        [Fact]
        public void CanAppendToEndOfFile()
        {
            this.AssertFileChange(
                TestUtilities.NormalizeNewlines("line1\nline2\nline3"),
                TestUtilities.NormalizeNewlines("line1\nline2\nline3\nline4\nline5"),
                new FileChange
                {
                    Line = 4,
                    EndLine = 5,
                    Offset = 1,
                    EndOffset = 1,
                    InsertString = $"line4{Environment.NewLine}line5"
                }
            );
        }

        [Fact]
        public void FindsDotSourcedFiles()
        {
            string exampleScriptContents = TestUtilities.PlatformNormalize(
                ". ./athing.ps1\n"+
                ". ./somefile.ps1\n"+
                ". ./somefile.ps1\n"+
                "Do-Stuff $uri\n"+
                ". simpleps.ps1");

            using (StringReader stringReader = new StringReader(exampleScriptContents))
            {
                ScriptFile scriptFile =
                    new ScriptFile(
                        "DotSourceTestFile.ps1",
                        "DotSourceTestFile.ps1",
                        stringReader,
                        PowerShellVersion);

                Assert.Equal(3, scriptFile.ReferencedFiles.Length);
                System.Console.Write("a" + scriptFile.ReferencedFiles[0]);
                Assert.Equal(TestUtilities.NormalizePath("./athing.ps1"), scriptFile.ReferencedFiles[0]);
            }
        }

        [Fact]
        public void ThrowsExceptionWithEditOutsideOfRange()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () =>
                {
                    this.AssertFileChange(
                        TestUtilities.NormalizeNewlines("first\nsecond\nREMOVE\nTHESE\nLINES\nthird"),
                        TestUtilities.NormalizeNewlines("first\nsecond\nthird"),
                        new FileChange
                        {
                            Line = 3,
                            EndLine = 8,
                            Offset = 1,
                            EndOffset = 1,
                            InsertString = ""
                        });
                });
        }

        [Fact]
        public void CanDeleteFromEndOfFile()
        {
            this.AssertFileChange(
                TestUtilities.NormalizeNewlines("line1\nline2\nline3\nline4"),
                TestUtilities.NormalizeNewlines("line1\nline2"),
                new FileChange
                {
                    Line = 3,
                    EndLine = 5,
                    Offset = 1,
                    EndOffset = 1,
                    InsertString = ""
                }
            );
        }

        internal static ScriptFile CreateScriptFile(string initialString)
        {
            using (StringReader stringReader = new StringReader(initialString))
            {
                // Create an in-memory file from the StringReader
                ScriptFile fileToChange =
                    new ScriptFile(
                        "TestFile.ps1",
                        "TestFile.ps1",
                        stringReader,
                        PowerShellVersion);

                return fileToChange;
            }
        }

        private void AssertFileChange(
            string initialString,
            string expectedString,
            FileChange fileChange)
        {
            // Create an in-memory file from the StringReader
            ScriptFile fileToChange = CreateScriptFile(initialString);

            // Apply the FileChange and assert the resulting contents
            fileToChange.ApplyChange(fileChange);
            Assert.Equal(expectedString, fileToChange.Contents);
        }
    }

    public class ScriptFileGetLinesTests
    {
        private static readonly string TestString_NoTrailingNewline = TestUtilities.NormalizeNewlines(
            "Line One\nLine Two\nLine Three\nLine Four\nLine Five");

        private static readonly string TestString_TrailingNewline = TestUtilities.NormalizeNewlines(
            TestString_NoTrailingNewline + "\n");

        private static readonly string[] s_newLines = new string[] { Environment.NewLine };

        private static readonly string[] s_testStringLines_noTrailingNewline = TestString_NoTrailingNewline.Split(s_newLines, StringSplitOptions.None);

        private static readonly string[] s_testStringLines_trailingNewline = TestString_TrailingNewline.Split(s_newLines, StringSplitOptions.None);

        private ScriptFile _scriptFile_trailingNewline;

        private ScriptFile _scriptFile_noTrailingNewline;

        public ScriptFileGetLinesTests()
        {
            _scriptFile_noTrailingNewline = ScriptFileChangeTests.CreateScriptFile(
                TestString_NoTrailingNewline);

            _scriptFile_trailingNewline = ScriptFileChangeTests.CreateScriptFile(
                TestString_TrailingNewline);
        }

        [Fact]
        public void CanGetWholeLine()
        {
            string[] lines =
                _scriptFile_noTrailingNewline.GetLinesInRange(
                    new BufferRange(5, 1, 5, 10));

            Assert.Single(lines);
            Assert.Equal("Line Five", lines[0]);
        }

        [Fact]
        public void CanGetMultipleWholeLines()
        {
            string[] lines =
                _scriptFile_noTrailingNewline.GetLinesInRange(
                    new BufferRange(2, 1, 4, 10));

            Assert.Equal(s_testStringLines_noTrailingNewline.Skip(1).Take(3), lines);
        }

        [Fact]
        public void CanGetSubstringInSingleLine()
        {
            string[] lines =
                _scriptFile_noTrailingNewline.GetLinesInRange(
                    new BufferRange(4, 3, 4, 8));

            Assert.Single(lines);
            Assert.Equal("ne Fo", lines[0]);
        }

        [Fact]
        public void CanGetEmptySubstringRange()
        {
            string[] lines =
                _scriptFile_noTrailingNewline.GetLinesInRange(
                    new BufferRange(4, 3, 4, 3));

            Assert.Single(lines);
            Assert.Equal("", lines[0]);
        }

        [Fact]
        public void CanGetSubstringInMultipleLines()
        {
            string[] expectedLines = new string[]
            {
                "Two",
                "Line Three",
                "Line Fou"
            };

            string[] lines =
                _scriptFile_noTrailingNewline.GetLinesInRange(
                    new BufferRange(2, 6, 4, 9));

            Assert.Equal(expectedLines, lines);
        }

        [Fact]
        public void CanGetRangeAtLineBoundaries()
        {
            string[] expectedLines = new string[]
            {
                "",
                "Line Three",
                ""
            };

            string[] lines =
                _scriptFile_noTrailingNewline.GetLinesInRange(
                    new BufferRange(2, 9, 4, 1));

            Assert.Equal(expectedLines, lines);
        }

        [Fact]
        public void CanSplitLines_NoTrailingNewline()
        {
            Assert.Equal(s_testStringLines_noTrailingNewline, _scriptFile_noTrailingNewline.FileLines);
        }

        [Fact]
        public void CanSplitLines_TrailingNewline()
        {
            Assert.Equal(s_testStringLines_trailingNewline, _scriptFile_trailingNewline.FileLines);
        }

        [Fact]
        public void CanGetSameLinesWithUnixLineBreaks()
        {
            var unixFile = ScriptFileChangeTests.CreateScriptFile(TestString_NoTrailingNewline.Replace("\r\n", "\n"));
            Assert.Equal(_scriptFile_noTrailingNewline.FileLines, unixFile.FileLines);
        }

        [Fact]
        public void CanGetLineForEmptyString()
        {
            var emptyFile = ScriptFileChangeTests.CreateScriptFile(string.Empty);
            Assert.Single(emptyFile.FileLines);
            Assert.Equal(string.Empty, emptyFile.FileLines[0]);
        }

        [Fact]
        public void CanGetLineForSpace()
        {
            var spaceFile = ScriptFileChangeTests.CreateScriptFile(" ");
            Assert.Single(spaceFile.FileLines);
            Assert.Equal(" ", spaceFile.FileLines[0]);
        }
    }

    public class ScriptFilePositionTests
    {
        private ScriptFile scriptFile;

        public ScriptFilePositionTests()
        {
            this.scriptFile =
                ScriptFileChangeTests.CreateScriptFile(@"
First line
  Second line is longer
    Third line
");
        }

        [Fact]
        public void CanOffsetByLine()
        {
            this.AssertNewPosition(
                1, 1,
                2, 0,
                3, 1);

            this.AssertNewPosition(
                3, 1,
                -2, 0,
                1, 1);
        }

        [Fact]
        public void CanOffsetByColumn()
        {
            this.AssertNewPosition(
                2, 1,
                0, 2,
                2, 3);

            this.AssertNewPosition(
                2, 5,
                0, -3,
                2, 2);
        }

        [Fact]
        public void ThrowsWhenPositionOutOfRange()
        {
            // Less than line range
            Assert.Throws<ArgumentOutOfRangeException>(
                () =>
                {
                    scriptFile.CalculatePosition(
                        new BufferPosition(1, 1),
                        -10, 0);
                });

            // Greater than line range
            Assert.Throws<ArgumentOutOfRangeException>(
                () =>
                {
                    scriptFile.CalculatePosition(
                        new BufferPosition(1, 1),
                        10, 0);
                });

            // Less than column range
            Assert.Throws<ArgumentOutOfRangeException>(
                () =>
                {
                    scriptFile.CalculatePosition(
                        new BufferPosition(1, 1),
                        0, -10);
                });

            // Greater than column range
            Assert.Throws<ArgumentOutOfRangeException>(
                () =>
                {
                    scriptFile.CalculatePosition(
                        new BufferPosition(1, 1),
                        0, 10);
                });
        }

        [Fact]
        public void CanFindBeginningOfLine()
        {
            this.AssertNewPosition(
                4, 12,
                pos => pos.GetLineStart(),
                4, 5);
        }

        [Fact]
        public void CanFindEndOfLine()
        {
            this.AssertNewPosition(
                4, 12,
                pos => pos.GetLineEnd(),
                4, 15);
        }

        [Fact]
        public void CanComposePositionOperations()
        {
            this.AssertNewPosition(
                4, 12,
                pos => pos.AddOffset(-1, 1).GetLineStart(),
                3, 3);
        }

        private void AssertNewPosition(
            int originalLine, int originalColumn,
            int lineOffset, int columnOffset,
            int expectedLine, int expectedColumn)
        {
            this.AssertNewPosition(
                originalLine, originalColumn,
                pos => pos.AddOffset(lineOffset, columnOffset),
                expectedLine, expectedColumn);
        }

        private void AssertNewPosition(
            int originalLine, int originalColumn,
            Func<FilePosition, FilePosition> positionOperation,
            int expectedLine, int expectedColumn)
        {
            var newPosition =
                positionOperation(
                    new FilePosition(
                        this.scriptFile,
                        originalLine,
                        originalColumn));

            Assert.Equal(expectedLine, newPosition.Line);
            Assert.Equal(expectedColumn, newPosition.Column);
        }
    }

    public class ScriptFileConstructorTests
    {
        private static readonly Version PowerShellVersion = new Version("5.0");

        [Fact]
        public void PropertiesInitializedCorrectlyForFile()
        {
            var path = "TestFile.ps1";
            var scriptFile = ScriptFileChangeTests.CreateScriptFile("");

            Assert.Equal(path, scriptFile.FilePath);
            Assert.Equal(path, scriptFile.ClientFilePath);
            Assert.True(scriptFile.IsAnalysisEnabled);
            Assert.False(scriptFile.IsInMemory);
            Assert.Empty(scriptFile.ReferencedFiles);
            Assert.Empty(scriptFile.SyntaxMarkers);
            Assert.Single(scriptFile.ScriptTokens);
            Assert.Single(scriptFile.FileLines);
        }

        [Fact]
        public void PropertiesInitializedCorrectlyForUntitled()
        {
            var path = "untitled:untitled-1";

            // 3 lines and 10 tokens in this script.
            var script = @"function foo() {
    'foo'
}";

            using (StringReader stringReader = new StringReader(script))
            {
                // Create an in-memory file from the StringReader
                var scriptFile = new ScriptFile(path, path, stringReader, PowerShellVersion);

                Assert.Equal(path, scriptFile.FilePath);
                Assert.Equal(path, scriptFile.ClientFilePath);
                Assert.True(scriptFile.IsAnalysisEnabled);
                Assert.True(scriptFile.IsInMemory);
                Assert.Empty(scriptFile.ReferencedFiles);
                Assert.Empty(scriptFile.SyntaxMarkers);
                Assert.Equal(10, scriptFile.ScriptTokens.Length);
                Assert.Equal(3, scriptFile.FileLines.Count);
            }
        }
    }
}
