using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Shouldly;
using System;
using System.IO;
using System.Linq;
using ToeicReading.Exams;
using ToeicReading.Exams.Dto;
using Xunit;

namespace ToeicReading.Tests.Exams
{
    public class WordExamImportParserTests
    {
        private readonly WordExamImportParser _parser = new WordExamImportParser();

        [Fact]
        public void InspectActualTaggedDocumentTemporarily()
        {
            var path = @"C:\Users\Tinh\Downloads\TOEIC_Reading_Test01_Tagged_Answered.docx";
            var preview = _parser.Parse(Path.GetFileName(path), File.ReadAllBytes(path));
            File.WriteAllText(
                @"C:\Users\Tinh\AppData\Local\Temp\toeic-tagged-parser-inspection.txt",
                string.Join(
                    Environment.NewLine,
                    preview.Errors.Select(issue => issue.Message)
                        .Concat(preview.Questions.SelectMany(question =>
                            new[]
                            {
                                $"Q{question.QuestionNumber} [{question.PartNumber}] TEXT={question.QuestionText}",
                                string.Join(" | ", question.Options.Select(option =>
                                    $"{option.Label}={option.Content}")),
                                string.Join(" | ", question.Errors.Select(issue => issue.Message))
                            }))));
        }

        [Fact]
        public void Parse_ShouldReadPart5QuestionAndOptions()
        {
            var preview = Parse(
                "[TITLE] Practice 01\n[DURATION] 75\n" +
                "[QUESTION PART=5 NUMBER=101]\nThe manager _____ the report.\n" +
                "A. review\nB. reviewed\nC. reviewing\nD. reviews\n" +
                "[ANSWER] B\n[END QUESTION]\n[END EXAM]");

            preview.IsValid.ShouldBeTrue();
            preview.Questions.Single().Answer.ShouldBe("B");
            preview.Questions.Single().Options.Count.ShouldBe(4);
        }

        [Fact]
        public void Parse_ShouldReadInlineOptionsWithCommonLabelFormats()
        {
            var preview = Parse(
                "[TITLE] Inline options\n[DURATION] 75\n" +
                "[QUESTION PART=5 NUMBER=102]\n" +
                "Lasner Electronics' staff have extensive ______ of current hardware systems. " +
                "A. know B) known (C) knowledge D. knowledgeable\n" +
                "[ANSWER] C\n[END QUESTION]\n[END EXAM]");

            var question = preview.Questions.Single();
            question.QuestionText.ShouldBe(
                "Lasner Electronics' staff have extensive ______ of current hardware systems.");
            question.Options.Select(option => option.Label)
                .ShouldBe(new[] { "A", "B", "C", "D" });
            question.Options.Select(option => option.Content)
                .ShouldBe(new[] { "know", "known", "knowledge", "knowledgeable" });
            preview.IsValid.ShouldBeTrue();
        }

        [Fact]
        public void Parse_ShouldReadSplitRunsTableOptionsAndContinuationParagraphs()
        {
            var preview = _parser.Parse(
                "table.docx",
                CreateDocxWithTableAndSplitRuns());

            var question = preview.Questions.Single();
            question.QuestionText.ShouldBe(
                "Lasner Electronics' staff have extensive ______ of current hardware systems.");
            question.Options.Select(option => option.Label)
                .ShouldBe(new[] { "A", "B", "C", "D" });
            question.Options[0].Content.ShouldBe("knowledge" + Environment.NewLine + "ledge");
            question.Options[1].Content.ShouldBe("known");
            question.Options[2].Content.ShouldBe("knowledge");
            question.Options[3].Content.ShouldBe("knowledgeable");
            question.Answer.ShouldBe("C");
            preview.IsValid.ShouldBeTrue();
        }

        [Fact]
        public void Parse_ShouldReadAutomaticallyNumberedWordOptions()
        {
            var preview = _parser.Parse(
                "numbered-options.docx",
                CreateDocxWithAutomaticallyNumberedOptions());

            var question = preview.Questions.Single();
            question.Options.Select(option => option.Label)
                .ShouldBe(new[] { "A", "B", "C", "D" });
            question.Options.Select(option => option.Content)
                .ShouldBe(new[] { "know", "known", "knowledge", "knowledgeable" });
            question.Answer.ShouldBe("C");
            preview.IsValid.ShouldBeTrue();
        }

        [Fact]
        public void Parse_ShouldNotDuplicateExplicitLabelFromAutomaticallyNumberedParagraph()
        {
            var preview = _parser.Parse(
                "numbered-options.docx",
                CreateDocxWithAutomaticallyNumberedOptions(explicitFirstLabel: true));

            var question = preview.Questions.Single();
            question.Options.Select(option => option.Label)
                .ShouldBe(new[] { "A", "B", "C", "D" });
            question.Options[0].Content.ShouldBe("know");
            question.Errors.Any(issue => issue.Code == "question.option.duplicate").ShouldBeFalse();
        }

        [Fact]
        public void Parse_ShouldReportGenuineDuplicateOptionLabels()
        {
            var preview = Parse(ValidExam.Replace(
                "B. reviewed",
                "A. reviewed"));

            preview.Questions[0].Errors.Any(issue => issue.Code == "question.option.duplicate")
                .ShouldBeTrue();
        }

        [Fact]
        public void Parse_ShouldLinkMultiplePart6QuestionsAndPart7Passages()
        {
            var preview = Parse(
                "[TITLE] Practice 02\n[DURATION] 75\n" +
                "[PASSAGE PART=6 TITLE=\"Announcement\"]\nThe office will close early.\n[END PASSAGE]\n" +
                "[QUESTION PART=6 NUMBER=131]\nWhat will happen?\nA. A\nB. B\nC. C\nD. D\n[ANSWER] B\n[END QUESTION]\n" +
                "[QUESTION PART=6 NUMBER=132]\nWhen will it happen?\nA. A\nB. B\nC. C\nD. D\n[ANSWER] A\n[END QUESTION]\n" +
                "[PASSAGE PART=7 TITLE=\"Email\"]\nPlease reply by Friday.\n[END PASSAGE]\n" +
                "[QUESTION PART=7 NUMBER=147]\nWhat is requested?\nA. A\nB. B\nC. C\nD. D\n[ANSWER] C\n[END QUESTION]\n[END EXAM]");

            preview.IsValid.ShouldBeTrue();
            preview.ReadingPassages.Count.ShouldBe(2);
            preview.Questions.Count.ShouldBe(3);
            preview.Questions[0].ReadingPassageTempId.ShouldBe(preview.Questions[1].ReadingPassageTempId);
            preview.Questions[2].ReadingPassageTempId.ShouldNotBe(preview.Questions[0].ReadingPassageTempId);
            preview.Questions[0].PartNumber.ShouldBe(6);
            preview.Questions[2].PartNumber.ShouldBe(7);
        }

        [Fact]
        public void Parse_ShouldReportMissingTitle()
        {
            var preview = Parse(ValidExam.Replace("[TITLE] Practice 01\n", string.Empty));

            preview.Errors.Any(issue => issue.Code == "title.required").ShouldBeTrue();
        }

        [Fact]
        public void Parse_ShouldReportMissingEndExam()
        {
            var preview = Parse(ValidExam.Replace("[END EXAM]", string.Empty));

            preview.Errors.Any(issue => issue.Code == "exam.end.missing").ShouldBeTrue();
        }

        [Fact]
        public void Parse_ShouldReportMissingOption()
        {
            var preview = Parse(ValidExam.Replace("D. reviews\n", string.Empty));

            preview.Questions[0].Errors.Any(issue => issue.Code == "question.option.missing").ShouldBeTrue();
        }

        [Fact]
        public void Parse_ShouldReportMissingQuestionText()
        {
            var preview = Parse(ValidExam.Replace("The manager _____ the report.\n", string.Empty));

            preview.Questions[0].Errors.Any(issue => issue.Code == "question.text.required")
                .ShouldBeTrue();
        }

        [Fact]
        public void Parse_ShouldRequireConfirmationForUnknownAnswer()
        {
            var preview = Parse(ValidExam.Replace("[ANSWER] B", "[ANSWER] UNKNOWN"));

            preview.Questions[0].Errors.Any(issue => issue.Code == "question.answer.required").ShouldBeTrue();
            preview.IsValid.ShouldBeFalse();
        }

        [Fact]
        public void Parse_ShouldReportUnsupportedPart()
        {
            var preview = Parse(ValidExam.Replace("PART=5", "PART=4"));

            preview.Questions[0].Errors.Any(issue => issue.Code == "question.part.invalid").ShouldBeTrue();
        }

        [Fact]
        public void Parse_ShouldReportDuplicateQuestionNumbers()
        {
            var duplicate = ValidExam.Replace(
                "[END EXAM]",
                "[QUESTION PART=5 NUMBER=101]\nAnother question?\nA. A\nB. B\nC. C\nD. D\n[ANSWER] A\n[END QUESTION]\n[END EXAM]");
            var preview = Parse(duplicate);

            preview.Errors.Any(issue => issue.Code == "question.number.duplicate").ShouldBeTrue();
        }

        [Fact]
        public void Parse_ShouldRejectCorruptDocx()
        {
            Should.Throw<WordExamImportException>(() =>
                _parser.Parse("broken.docx", new byte[] { 1, 2, 3, 4 }));
        }

        [Fact]
        public void Parse_ShouldRejectNonDocxExtension()
        {
            Should.Throw<WordExamImportException>(() =>
                _parser.Parse("practice.doc", CreateDocx(ValidExam)));
        }

        private WordExamPreviewDto Parse(string text)
        {
            return _parser.Parse("practice.docx", CreateDocx(text));
        }

        private static byte[] CreateDocx(string text)
        {
            using (var stream = new MemoryStream())
            {
                using (var document = WordprocessingDocument.Create(
                           stream,
                           WordprocessingDocumentType.Document,
                           true))
                {
                    var mainPart = document.AddMainDocumentPart();
                    var body = new Body();
                    foreach (var line in text.Split('\n'))
                    {
                        body.AppendChild(new Paragraph(
                            new Run(new Text(line)
                            {
                                Space = SpaceProcessingModeValues.Preserve
                            })));
                    }

                    mainPart.Document = new Document(body);
                    mainPart.Document.Save();
                }

                return stream.ToArray();
            }
        }

        private static byte[] CreateDocxWithTableAndSplitRuns()
        {
            using (var stream = new MemoryStream())
            {
                using (var document = WordprocessingDocument.Create(
                           stream,
                           WordprocessingDocumentType.Document,
                           true))
                {
                    var body = new Body();
                    body.AppendChild(CreateParagraph("[TITLE] Table options"));
                    body.AppendChild(CreateParagraph("[DURATION] 75"));
                    body.AppendChild(CreateParagraph("[QUESTION PART=5 NUMBER=102]"));
                    body.AppendChild(new Paragraph(
                        new Run(new Text("Lasner Electronics' staff have extensive ")),
                        new Run(new Text("______ of current hardware systems."))));

                    var table = new Table();
                    table.AppendChild(CreateOptionRow("A.", "know", "ledge"));
                    table.AppendChild(CreateOptionRow("B)", "known"));
                    table.AppendChild(CreateOptionRow("(C)", "knowledge"));
                    table.AppendChild(CreateOptionRow("D.", "knowledge", "able"));
                    body.AppendChild(table);
                    body.AppendChild(CreateParagraph("[ANSWER] C"));
                    body.AppendChild(CreateParagraph("[END QUESTION]"));
                    body.AppendChild(CreateParagraph("[END EXAM]"));

                    var mainPart = document.AddMainDocumentPart();
                    mainPart.Document = new Document(body);
                    mainPart.Document.Save();
                }

                return stream.ToArray();
            }
        }

        private static byte[] CreateDocxWithAutomaticallyNumberedOptions(bool explicitFirstLabel = false)
        {
            using (var stream = new MemoryStream())
            {
                using (var document = WordprocessingDocument.Create(
                           stream,
                           WordprocessingDocumentType.Document,
                           true))
                {
                    var mainPart = document.AddMainDocumentPart();
                    var numberingPart = mainPart.AddNewPart<NumberingDefinitionsPart>();
                    numberingPart.Numbering = new Numbering(
                        new AbstractNum(
                            new Level(
                                new StartNumberingValue { Val = 1 },
                                new NumberingFormat { Val = NumberFormatValues.UpperLetter },
                                new LevelText { Val = "(%1)" },
                                new LevelJustification { Val = LevelJustificationValues.Left })
                            {
                                LevelIndex = 0
                            })
                        {
                            AbstractNumberId = 0
                        },
                        new NumberingInstance(new AbstractNumId { Val = 0 })
                        {
                            NumberID = 1
                        });

                    var body = new Body();
                    body.AppendChild(CreateParagraph("[TITLE] Automatically numbered options"));
                    body.AppendChild(CreateParagraph("[DURATION] 75"));
                    body.AppendChild(CreateParagraph("[QUESTION PART=5 NUMBER=102]"));
                    body.AppendChild(CreateParagraph(
                        "Lasner Electronics' staff have extensive ______ of current hardware systems."));
                    body.AppendChild(CreateNumberedParagraph(
                        1,
                        explicitFirstLabel ? "(A) know" : "know"));
                    body.AppendChild(CreateNumberedParagraph(1, "known"));
                    body.AppendChild(CreateNumberedParagraph(1, "knowledge"));
                    body.AppendChild(CreateNumberedParagraph(1, "knowledgeable"));
                    body.AppendChild(CreateParagraph("[ANSWER] C"));
                    body.AppendChild(CreateParagraph("[END QUESTION]"));
                    body.AppendChild(CreateParagraph("[END EXAM]"));
                    mainPart.Document = new Document(body);
                    numberingPart.Numbering.Save();
                    mainPart.Document.Save();
                }

                return stream.ToArray();
            }
        }

        private static Paragraph CreateNumberedParagraph(int numberId, string text)
        {
            return new Paragraph(
                new ParagraphProperties(
                    new NumberingProperties(
                        new NumberingLevelReference { Val = 0 },
                        new NumberingId { Val = numberId })),
                new Run(new Text(text)
                {
                    Space = SpaceProcessingModeValues.Preserve
                }));
        }

        private static TableRow CreateOptionRow(string label, params string[] contentRuns)
        {
            var contentParagraph = new Paragraph(
                contentRuns.Select(value =>
                    (OpenXmlElement)new Run(new Text(value)
                    {
                        Space = SpaceProcessingModeValues.Preserve
                    })));
            var contentCell = new TableCell(contentParagraph);
            if (label == "A.")
            {
                contentCell.AppendChild(CreateParagraph("ledge"));
            }

            return new TableRow(
                new TableCell(CreateParagraph(label)),
                contentCell);
        }

        private static Paragraph CreateParagraph(string text)
        {
            return new Paragraph(new Run(new Text(text)
            {
                Space = SpaceProcessingModeValues.Preserve
            }));
        }

        private const string ValidExam =
            "[TITLE] Practice 01\n[DURATION] 75\n" +
            "[QUESTION PART=5 NUMBER=101]\nThe manager _____ the report.\n" +
            "A. review\nB. reviewed\nC. reviewing\nD. reviews\n" +
            "[ANSWER] B\n[END QUESTION]\n[END EXAM]";
    }
}
