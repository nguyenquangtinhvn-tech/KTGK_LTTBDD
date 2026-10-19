using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Abp.Dependency;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using ToeicReading.Exams.Dto;

namespace ToeicReading.Exams
{
    public interface IWordExamImportParser
    {
        WordExamPreviewDto Parse(string fileName, byte[] content);

        List<WordImportIssueDto> Validate(WordExamPreviewDto preview);
    }

    public class WordExamImportParser : IWordExamImportParser, ITransientDependency
    {
        public const int MaxFileSizeBytes = 10 * 1024 * 1024;

        private static readonly Regex TagRegex = new Regex(
            @"^\[(?<tag>END EXAM|END PASSAGE|END QUESTION|PASSAGE|QUESTION|TITLE|DESCRIPTION|DURATION|ANSWER|EXPLANATION|HINT)\b(?<attributes>[^\]]*)\]\s*(?<value>.*)$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex AttributeRegex = new Regex(
            @"(?<name>[A-Z]+)\s*=\s*(?:""(?<quoted>[^""]*)""|(?<plain>[^\s]+))",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex OptionCandidateRegex = new Regex(
            @"^\s*(?:\((?<parenthesized>[A-Z])\)|(?<plain>[A-Z])[.)])\s*(?<content>.*)$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex InlineOptionRegex = new Regex(
            @"(?<![A-Za-z0-9])(?:\((?<parenthesized>[A-D])\)|(?<plain>[A-D])[.)])(?=\s)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex NumberedInlineOptionGroupRegex = new Regex(
            @"(?<!\d)(?<number>\d{3})[.)]\s*(?=(?:\([A-D]\)|[A-D][.)])\s)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex NumberingPlaceholderRegex = new Regex(
            @"%([1-9])",
            RegexOptions.Compiled);

        public WordExamPreviewDto Parse(string fileName, byte[] content)
        {
            if (string.IsNullOrWhiteSpace(fileName) ||
                !string.Equals(Path.GetExtension(fileName), ".docx", StringComparison.OrdinalIgnoreCase))
            {
                throw new WordExamImportException("Chỉ chấp nhận tệp Word có định dạng .docx.");
            }

            if (content == null || content.Length == 0)
            {
                throw new WordExamImportException("Tệp DOCX rỗng.");
            }

            if (content.Length > MaxFileSizeBytes)
            {
                throw new WordExamImportException("Tệp DOCX vượt quá giới hạn 10 MB.");
            }

            var lines = ReadDocxLines(content);
            return ParseLines(lines);
        }

        public List<WordImportIssueDto> Validate(WordExamPreviewDto preview)
        {
            var issues = new List<WordImportIssueDto>();
            if (preview == null)
            {
                issues.Add(Issue("preview.required", "Dữ liệu xem trước không hợp lệ."));
                return issues;
            }

            if (string.IsNullOrWhiteSpace(preview.Title))
            {
                issues.Add(Issue("title.required", "Thiếu tiêu đề đề thi [TITLE]."));
            }
            else if (preview.Title.Trim().Length > 200)
            {
                issues.Add(Issue("title.length", "Tiêu đề đề thi không được vượt quá 200 ký tự."));
            }

            if (preview.DurationMinutes < 1 || preview.DurationMinutes > 300)
            {
                issues.Add(Issue("duration.invalid", "Thời lượng [DURATION] phải từ 1 đến 300 phút."));
            }

            if (!preview.HasEndExam)
            {
                issues.Add(Issue("exam.end.missing", "Thiếu thẻ bắt buộc [END EXAM]."));
            }

            if (preview.Questions == null || preview.Questions.Count == 0)
            {
                issues.Add(Issue("questions.required", "Đề thi phải có ít nhất một câu hỏi."));
            }

            var passages = preview.ReadingPassages ?? new List<WordReadingPassageDto>();
            var questions = preview.Questions ?? new List<WordQuestionDto>();
            if (passages.Any(passage => passage == null))
            {
                issues.Add(Issue("passage.invalid", "Danh sách bài đọc chứa phần tử không hợp lệ."));
            }
            if (questions.Any(question => question == null))
            {
                issues.Add(Issue("question.invalid", "Danh sách câu hỏi chứa phần tử không hợp lệ."));
            }

            passages = passages.Where(passage => passage != null).ToList();
            questions = questions.Where(question => question != null).ToList();
            var duplicatePassageIds = passages
                .Where(passage => !string.IsNullOrWhiteSpace(passage.TempId))
                .GroupBy(passage => passage.TempId, StringComparer.Ordinal)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key)
                .ToHashSet(StringComparer.Ordinal);

            foreach (var passage in passages)
            {
                if (string.IsNullOrWhiteSpace(passage.TempId) || duplicatePassageIds.Contains(passage.TempId))
                {
                    issues.Add(Issue("passage.id.invalid", "Mã tạm của bài đọc bị thiếu hoặc trùng."));
                }
                if (passage.PartNumber != 6 && passage.PartNumber != 7)
                {
                    issues.Add(Issue("passage.part.invalid", "Bài đọc chỉ hỗ trợ Part 6 hoặc Part 7."));
                }
                if (string.IsNullOrWhiteSpace(passage.Title) || string.IsNullOrWhiteSpace(passage.Content))
                {
                    issues.Add(Issue("passage.content.required", "Mỗi bài đọc cần có tiêu đề và nội dung."));
                }
            }

            var passageById = passages
                .Where(passage => !string.IsNullOrWhiteSpace(passage.TempId))
                .GroupBy(passage => passage.TempId, StringComparer.Ordinal)
                .Where(group => group.Count() == 1)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);

            foreach (var duplicate in questions
                         .Where(question => question.QuestionNumber > 0)
                         .GroupBy(question => question.QuestionNumber)
                         .Where(group => group.Count() > 1))
            {
                issues.Add(Issue(
                    "question.number.duplicate",
                    $"Số câu {duplicate.Key} bị trùng trong đề thi."));
            }

            foreach (var question in questions)
            {
                if (question.QuestionNumber <= 0)
                {
                    issues.Add(Issue("question.number.invalid", "Số câu hỏi phải là số nguyên dương.", question.QuestionNumber));
                }
                if (question.PartNumber != 5 && question.PartNumber != 6 && question.PartNumber != 7)
                {
                    issues.Add(Issue("question.part.invalid", "Phần thi chỉ được là Part 5, Part 6 hoặc Part 7.", question.QuestionNumber));
                }
                if (question.PartNumber == 5 && string.IsNullOrWhiteSpace(question.QuestionText))
                {
                    issues.Add(Issue("question.text.required", "Thiếu nội dung câu hỏi.", question.QuestionNumber));
                }

                var normalizedOptions = (question.Options ?? new List<WordQuestionOptionDto>())
                    .Where(option => option != null)
                    .Select(option => new
                    {
                        Label = (option.Label ?? string.Empty).Trim().ToUpperInvariant(),
                        Content = option.Content
                    })
                    .ToList();

                if ((question.Options ?? new List<WordQuestionOptionDto>()).Any(option => option == null))
                {
                    issues.Add(Issue(
                        "question.option.invalid",
                        $"Câu {question.QuestionNumber}: danh sách phương án chứa phần tử không hợp lệ.",
                        question.QuestionNumber));
                }

                if (normalizedOptions.Any(option =>
                        option.Label != "A" && option.Label != "B" && option.Label != "C" && option.Label != "D"))
                {
                    issues.Add(Issue(
                        "question.option.label.invalid",
                        $"Câu {question.QuestionNumber}: nhãn phương án chỉ được là A, B, C hoặc D.",
                        question.QuestionNumber));
                }

                foreach (var label in new[] { "A", "B", "C", "D" })
                {
                    if (!normalizedOptions.Any(option => option.Label == label && !string.IsNullOrWhiteSpace(option.Content)))
                    {
                        issues.Add(Issue(
                            "question.option.missing",
                            $"Câu {question.QuestionNumber}: thiếu hoặc để trống phương án {label}.",
                            question.QuestionNumber));
                    }
                }

                if (normalizedOptions
                    .Where(option => option.Label == "A" || option.Label == "B" || option.Label == "C" || option.Label == "D")
                    .GroupBy(option => option.Label)
                    .Any(group => group.Count() > 1))
                {
                    issues.Add(Issue("question.option.duplicate", $"Câu {question.QuestionNumber}: nhãn phương án A/B/C/D không được trùng.", question.QuestionNumber));
                }

                var answer = (question.Answer ?? string.Empty).Trim().ToUpperInvariant();
                if (answer != "A" && answer != "B" && answer != "C" && answer != "D")
                {
                    issues.Add(Issue(
                        "question.answer.required",
                        $"Câu {question.QuestionNumber}: đáp án thiếu, UNKNOWN hoặc không hợp lệ; hãy xác nhận A, B, C hoặc D.",
                        question.QuestionNumber));
                }
                else if (!normalizedOptions.Any(option => option.Label == answer && !string.IsNullOrWhiteSpace(option.Content)))
                {
                    issues.Add(Issue(
                        "question.answer.option_missing",
                        $"Câu {question.QuestionNumber}: đáp án {answer} chưa có nội dung phương án.",
                        question.QuestionNumber));
                }

                if (question.PartNumber == 5)
                {
                    if (!string.IsNullOrWhiteSpace(question.ReadingPassageTempId))
                    {
                        issues.Add(Issue("question.passage.unexpected", $"Câu Part 5 số {question.QuestionNumber} không được liên kết bài đọc.", question.QuestionNumber));
                    }
                }
                else if (string.IsNullOrWhiteSpace(question.ReadingPassageTempId) ||
                         !passageById.TryGetValue(question.ReadingPassageTempId, out var passage) ||
                         passage.PartNumber != question.PartNumber)
                {
                    issues.Add(Issue(
                        "question.passage.required",
                        $"Câu Part {question.PartNumber} số {question.QuestionNumber} phải liên kết với bài đọc cùng Part.",
                        question.QuestionNumber));
                }
            }

            return issues;
        }

        private static List<string> ReadDocxLines(byte[] content)
        {
            try
            {
                using (var stream = new MemoryStream(content, false))
                using (var document = WordprocessingDocument.Open(stream, false))
                {
                    var body = document.MainDocumentPart?.Document?.Body;
                    if (body == null)
                    {
                        throw new WordExamImportException("Tệp DOCX không có nội dung tài liệu.");
                    }

                    var lines = new List<string>();
                    var numberingCounters = new Dictionary<int, Dictionary<int, int>>();
                    foreach (var paragraph in body.Descendants<Paragraph>())
                    {
                        var text = new StringBuilder();
                        foreach (var element in paragraph.Descendants<OpenXmlElement>())
                        {
                            if (element is Text runText)
                            {
                                text.Append(runText.Text);
                            }
                            else if (element is Break)
                            {
                                text.Append('\n');
                            }
                            else if (element is TabChar)
                            {
                                text.Append('\t');
                            }
                        }

                        var paragraphText = text.ToString();
                        var numberingLabel = GetLetterNumberingLabel(
                            paragraph,
                            document.MainDocumentPart.NumberingDefinitionsPart,
                            numberingCounters);
                        if (!string.IsNullOrEmpty(numberingLabel) &&
                            !OptionCandidateRegex.IsMatch(paragraphText))
                        {
                            paragraphText = numberingLabel + " " + paragraphText;
                        }

                        lines.AddRange(paragraphText.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n'));
                    }

                    return lines;
                }
            }
            catch (WordExamImportException)
            {
                throw;
            }
            catch (OpenXmlPackageException)
            {
                throw new WordExamImportException("Tệp DOCX bị hỏng hoặc không đúng cấu trúc Word.");
            }
            catch (FileFormatException)
            {
                throw new WordExamImportException("Tệp DOCX bị hỏng hoặc không đúng cấu trúc Word.");
            }
            catch (XmlException)
            {
                throw new WordExamImportException("Tệp DOCX bị hỏng hoặc không đúng cấu trúc Word.");
            }
            catch (InvalidDataException)
            {
                throw new WordExamImportException("Tệp DOCX bị hỏng hoặc không đúng cấu trúc Word.");
            }
        }

        private static string GetLetterNumberingLabel(
            Paragraph paragraph,
            NumberingDefinitionsPart numberingPart,
            IDictionary<int, Dictionary<int, int>> counters)
        {
            var numberingProperties = paragraph.ParagraphProperties?.NumberingProperties;
            var numberId = numberingProperties?.NumberingId?.Val?.Value;
            if (!numberId.HasValue || numberId.Value == 0 || numberingPart?.Numbering == null)
            {
                return null;
            }

            var levelIndex = numberingProperties.NumberingLevelReference?.Val?.Value ?? 0;
            if (levelIndex < 0 || levelIndex > 8)
            {
                return null;
            }

            var numberingInstance = numberingPart.Numbering
                .Elements<NumberingInstance>()
                .FirstOrDefault(item => item.NumberID?.Value == numberId.Value);
            var abstractNumberId = numberingInstance?.AbstractNumId?.Val?.Value;
            if (!abstractNumberId.HasValue)
            {
                return null;
            }

            var abstractNumber = numberingPart.Numbering
                .Elements<AbstractNum>()
                .FirstOrDefault(item => item.AbstractNumberId?.Value == abstractNumberId.Value);
            var levelOverride = numberingInstance.Elements<LevelOverride>()
                .FirstOrDefault(item => item.LevelIndex?.Value == levelIndex);
            var level = levelOverride?.Level ??
                        abstractNumber?.Elements<Level>()
                            .FirstOrDefault(item => item.LevelIndex?.Value == levelIndex);
            var numberFormat = level?.NumberingFormat?.Val;
            var levelText = level?.LevelText?.Val;
            if (numberFormat != NumberFormatValues.UpperLetter &&
                numberFormat != NumberFormatValues.LowerLetter ||
                string.IsNullOrWhiteSpace(levelText))
            {
                return null;
            }

            if (!counters.TryGetValue(numberId.Value, out var levelCounters))
            {
                levelCounters = new Dictionary<int, int>();
                counters[numberId.Value] = levelCounters;
            }

            var start = levelOverride?.StartOverrideNumberingValue?.Val?.Value ??
                        level?.StartNumberingValue?.Val?.Value ??
                        1;
            levelCounters[levelIndex] = levelCounters.TryGetValue(levelIndex, out var current)
                ? current + 1
                : start;
            foreach (var deeperLevel in levelCounters.Keys.Where(index => index > levelIndex).ToList())
            {
                levelCounters.Remove(deeperLevel);
            }

            var currentLevelValue = levelCounters[levelIndex];
            var expectedFormat = numberFormat.Value;
            return NumberingPlaceholderRegex.Replace(levelText, match =>
            {
                var referencedLevel = int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture) - 1;
                var value = levelCounters.TryGetValue(referencedLevel, out var referencedValue)
                    ? referencedValue
                    : referencedLevel == levelIndex
                        ? currentLevelValue
                        : 1;
                var referencedFormat = referencedLevel == levelIndex
                    ? expectedFormat
                    : GetNumberingFormat(numberingInstance, abstractNumber, referencedLevel);
                if (referencedFormat == NumberFormatValues.UpperLetter)
                {
                    return ToAlphabeticNumber(value, true);
                }
                if (referencedFormat == NumberFormatValues.LowerLetter)
                {
                    return ToAlphabeticNumber(value, false);
                }

                return value.ToString(CultureInfo.InvariantCulture);
            });
        }

        private static NumberFormatValues? GetNumberingFormat(
            NumberingInstance numberingInstance,
            AbstractNum abstractNumber,
            int levelIndex)
        {
            var overrideLevel = numberingInstance.Elements<LevelOverride>()
                .FirstOrDefault(item => item.LevelIndex?.Value == levelIndex);
            return (overrideLevel?.Level ??
                    abstractNumber?.Elements<Level>()
                        .FirstOrDefault(item => item.LevelIndex?.Value == levelIndex))
                ?.NumberingFormat?.Val;
        }

        private static string ToAlphabeticNumber(int value, bool uppercase)
        {
            if (value <= 0)
            {
                return value.ToString(CultureInfo.InvariantCulture);
            }

            var label = new StringBuilder();
            while (value > 0)
            {
                value--;
                label.Insert(0, (char)('A' + value % 26));
                value /= 26;
            }

            var result = label.ToString();
            return uppercase ? result : result.ToLowerInvariant();
        }

        private WordExamPreviewDto ParseLines(IEnumerable<string> inputLines)
        {
            var preview = new WordExamPreviewDto();
            WordReadingPassageDto currentPassage = null;
            WordQuestionDto currentQuestion = null;
            var activePassageByPart = new Dictionary<int, string>();
            var questionsByNumber = new Dictionary<int, WordQuestionDto>();
            var pendingOptionsByQuestionNumber = new Dictionary<int, List<WordQuestionOptionDto>>();
            var section = "root";
            var hasEndExam = false;
            var hasStartedExamStructure = false;
            var passageIndex = 0;
            int? activeOptionQuestionNumber = null;

            foreach (var rawLine in inputLines)
            {
                var line = (rawLine ?? string.Empty).Trim();
                if (line.Length == 0)
                {
                    AppendContinuation(section, line, preview, currentPassage, currentQuestion);
                    continue;
                }

                if (hasEndExam)
                {
                    preview.Warnings.Add(Issue(
                        "document.content.after_end",
                        "Có nội dung sau [END EXAM]; nội dung này không được nhập."));
                    continue;
                }

                var tagMatch = TagRegex.Match(line);
                if (!tagMatch.Success)
                {
                    if (line.StartsWith("[", StringComparison.Ordinal))
                    {
                        preview.Errors.Add(Issue("document.tag.invalid", $"Thẻ hoặc dòng tag không hợp lệ: {line}"));
                        continue;
                    }

                    if (section == "root" && !hasStartedExamStructure && !string.IsNullOrWhiteSpace(line))
                    {
                        preview.Warnings.Add(Issue(
                            "document.preamble.unwrapped",
                            $"Có metadata chưa được gắn thẻ trước nội dung đề thi: {line}"));
                        continue;
                    }

                    var numberedOptionGroups = NumberedInlineOptionGroupRegex.Matches(line);
                    if (currentQuestion != null && numberedOptionGroups.Count > 0)
                    {
                        var textBeforeGroups = line.Substring(0, numberedOptionGroups[0].Index).Trim();
                        if (textBeforeGroups.Length > 0)
                        {
                            if (section == "options" && activeOptionQuestionNumber.HasValue)
                            {
                                AppendToLastOption(
                                    GetOptions(activeOptionQuestionNumber.Value, questionsByNumber, pendingOptionsByQuestionNumber),
                                    textBeforeGroups);
                            }
                            else
                            {
                                currentQuestion.QuestionText = AppendLine(
                                    currentQuestion.QuestionText,
                                    textBeforeGroups);
                            }
                        }

                        for (var groupIndex = 0; groupIndex < numberedOptionGroups.Count; groupIndex++)
                        {
                            var group = numberedOptionGroups[groupIndex];
                            var targetQuestionNumber = int.Parse(
                                group.Groups["number"].Value,
                                CultureInfo.InvariantCulture);
                            var contentStart = group.Index + group.Length;
                            var contentEnd = groupIndex + 1 < numberedOptionGroups.Count
                                ? numberedOptionGroups[groupIndex + 1].Index
                                : line.Length;
                            var optionText = line.Substring(contentStart, contentEnd - contentStart);
                            AddInlineOptions(
                                optionText,
                                GetOptions(targetQuestionNumber, questionsByNumber, pendingOptionsByQuestionNumber));
                            activeOptionQuestionNumber = targetQuestionNumber;
                        }

                        section = "options";
                        continue;
                    }

                    if (currentQuestion != null &&
                        (section == "question" || section == "options"))
                    {
                        var inlineOptions = InlineOptionRegex.Matches(line);
                        if (inlineOptions.Count > 1)
                        {
                            var firstOption = inlineOptions[0];
                            var textBeforeOptions = line.Substring(0, firstOption.Index).Trim();
                            if (textBeforeOptions.Length > 0)
                            {
                                if (section == "options" && activeOptionQuestionNumber.HasValue)
                                {
                                    AppendToLastOption(
                                        GetOptions(activeOptionQuestionNumber.Value, questionsByNumber, pendingOptionsByQuestionNumber),
                                        textBeforeOptions);
                                }
                                else if (section == "question")
                                {
                                    currentQuestion.QuestionText = AppendLine(
                                        currentQuestion.QuestionText,
                                        textBeforeOptions);
                                }
                                else
                                {
                                    var previousOption = currentQuestion.Options.LastOrDefault();
                                    if (previousOption != null)
                                    {
                                        previousOption.Content = AppendLine(
                                            previousOption.Content,
                                            textBeforeOptions);
                                    }
                                }
                            }

                            var optionText = line.Substring(firstOption.Index);
                            var targetOptions = activeOptionQuestionNumber.HasValue
                                ? GetOptions(activeOptionQuestionNumber.Value, questionsByNumber, pendingOptionsByQuestionNumber)
                                : currentQuestion.Options;
                            AddInlineOptions(optionText, targetOptions);
                            section = "options";
                        }
                        else
                        {
                            var optionCandidate = OptionCandidateRegex.Match(line);
                            if (optionCandidate.Success)
                            {
                                var targetQuestionNumber = activeOptionQuestionNumber ?? currentQuestion.QuestionNumber;
                                var targetOptions = GetOptions(
                                    targetQuestionNumber,
                                    questionsByNumber,
                                    pendingOptionsByQuestionNumber);
                                var label = optionCandidate.Groups["parenthesized"].Success
                                    ? optionCandidate.Groups["parenthesized"].Value
                                    : optionCandidate.Groups["plain"].Value;
                                if (!IsSupportedOptionLabel(label))
                                {
                                    var targetQuestion = questionsByNumber.TryGetValue(targetQuestionNumber, out var foundQuestion)
                                        ? foundQuestion
                                        : currentQuestion;
                                    targetQuestion.Errors.Add(Issue(
                                        "question.option.label.invalid",
                                        $"Câu {targetQuestionNumber}: nhãn phương án phải là A, B, C hoặc D.",
                                        targetQuestionNumber));
                                }
                                else
                                {
                                    targetOptions.Add(new WordQuestionOptionDto
                                    {
                                        Label = label.ToUpperInvariant(),
                                        Content = optionCandidate.Groups["content"].Value.Trim()
                                    });
                                }
                                section = "options";
                                continue;
                            }

                            if (section == "options" && activeOptionQuestionNumber.HasValue)
                            {
                                AppendToLastOption(
                                    GetOptions(activeOptionQuestionNumber.Value, questionsByNumber, pendingOptionsByQuestionNumber),
                                    line);
                                continue;
                            }

                            AppendContinuation(section, line, preview, currentPassage, currentQuestion);
                        }
                        continue;
                    }

                    AppendContinuation(section, line, preview, currentPassage, currentQuestion);
                    continue;
                }

                var tag = tagMatch.Groups["tag"].Value.Trim().ToUpperInvariant();
                var attributes = ParseAttributes(tagMatch.Groups["attributes"].Value);
                var value = tagMatch.Groups["value"].Value.Trim();
                hasStartedExamStructure = true;

                if (tag == "END EXAM")
                {
                    if (currentPassage != null || currentQuestion != null)
                    {
                        preview.Errors.Add(Issue("document.block.unclosed", "[END EXAM] xuất hiện khi bài đọc hoặc câu hỏi chưa kết thúc."));
                    }
                    hasEndExam = true;
                    preview.HasEndExam = true;
                    section = "ended";
                    continue;
                }

                if (tag == "TITLE")
                {
                    if (string.IsNullOrWhiteSpace(preview.Title))
                    {
                        preview.Title = value;
                    }
                    else
                    {
                        preview.Errors.Add(Issue("title.duplicate", "Chỉ được có một thẻ [TITLE]."));
                    }
                    section = "root";
                }
                else if (tag == "DESCRIPTION")
                {
                    preview.Description = value;
                    section = "description";
                }
                else if (tag == "DURATION")
                {
                    if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var duration))
                    {
                        preview.Errors.Add(Issue("duration.invalid", "[DURATION] phải là số phút hợp lệ."));
                    }
                    else
                    {
                        preview.DurationMinutes = duration;
                    }
                    section = "root";
                }
                else if (tag == "PASSAGE")
                {
                    activeOptionQuestionNumber = null;
                    if (currentPassage != null || currentQuestion != null)
                    {
                        preview.Errors.Add(Issue("passage.nested", "Không thể mở [PASSAGE] bên trong một bài đọc hoặc câu hỏi khác."));
                        continue;
                    }

                    var partNumber = ParseIntegerAttribute(attributes, "PART");
                    var title = GetAttribute(attributes, "TITLE");
                    passageIndex++;
                    currentPassage = new WordReadingPassageDto
                    {
                        TempId = $"passage-{passageIndex}",
                        PartNumber = partNumber,
                        Title = title,
                        OrderNumber = passageIndex
                    };
                    preview.ReadingPassages.Add(currentPassage);
                    if (partNumber == 6 || partNumber == 7)
                    {
                        activePassageByPart[partNumber] = currentPassage.TempId;
                    }
                    else
                    {
                        preview.Errors.Add(Issue("passage.part.invalid", "Bài đọc chỉ hỗ trợ PART=6 hoặc PART=7."));
                    }
                    if (string.IsNullOrWhiteSpace(title))
                    {
                        preview.Errors.Add(Issue("passage.title.required", $"Bài đọc thứ {passageIndex} thiếu TITLE."));
                    }
                    section = "passage";
                }
                else if (tag == "END PASSAGE")
                {
                    if (currentPassage == null)
                    {
                        preview.Errors.Add(Issue("passage.end.unexpected", "[END PASSAGE] không có bài đọc đang mở."));
                    }
                    else
                    {
                        if (string.IsNullOrWhiteSpace(currentPassage.Content))
                        {
                            preview.Errors.Add(Issue("passage.content.required", $"Bài đọc \"{currentPassage.Title}\" chưa có nội dung."));
                        }
                        currentPassage = null;
                    }
                    section = "root";
                }
                else if (tag == "QUESTION")
                {
                    activeOptionQuestionNumber = null;
                    if (currentQuestion != null || currentPassage != null)
                    {
                        preview.Errors.Add(Issue("question.nested", "Không thể mở [QUESTION] khi bài đọc hoặc câu hỏi khác chưa kết thúc."));
                        continue;
                    }

                    var partNumber = ParseIntegerAttribute(attributes, "PART");
                    var questionNumber = ParseIntegerAttribute(attributes, "NUMBER");
                    currentQuestion = new WordQuestionDto
                    {
                        PartNumber = partNumber,
                        QuestionNumber = questionNumber,
                        ReadingPassageTempId = partNumber == 5
                            ? null
                            : activePassageByPart.TryGetValue(partNumber, out var passageId)
                                ? passageId
                                : null
                    };
                    preview.Questions.Add(currentQuestion);
                    if (questionNumber > 0 && !questionsByNumber.ContainsKey(questionNumber))
                    {
                        questionsByNumber.Add(questionNumber, currentQuestion);
                    }
                    if (pendingOptionsByQuestionNumber.TryGetValue(questionNumber, out var pendingOptions))
                    {
                        currentQuestion.Options.AddRange(pendingOptions);
                        pendingOptionsByQuestionNumber.Remove(questionNumber);
                    }
                    if (partNumber != 5 && partNumber != 6 && partNumber != 7)
                    {
                        currentQuestion.Errors.Add(Issue(
                            "question.part.invalid",
                            $"Câu {questionNumber}: PART chỉ được là 5, 6 hoặc 7.",
                            questionNumber));
                    }
                    section = "question";
                }
                else if (tag == "END QUESTION")
                {
                    activeOptionQuestionNumber = null;
                    if (currentQuestion == null)
                    {
                        preview.Errors.Add(Issue("question.end.unexpected", "[END QUESTION] không có câu hỏi đang mở."));
                    }
                    currentQuestion = null;
                    section = "root";
                }
                else if (currentQuestion != null && tag == "ANSWER")
                {
                    currentQuestion.Answer = value;
                    activeOptionQuestionNumber = null;
                    section = "answer";
                }
                else if (currentQuestion != null && tag == "EXPLANATION")
                {
                    currentQuestion.Explanation = value;
                    section = "explanation";
                }
                else if (currentQuestion != null && tag == "HINT")
                {
                    currentQuestion.Hint = value;
                    section = "hint";
                }
                else
                {
                    preview.Errors.Add(Issue("document.tag.unknown", $"Thẻ [{tag}] không hợp lệ tại vị trí này."));
                }
            }

            if (currentPassage != null)
            {
                preview.Errors.Add(Issue("passage.end.missing", $"Bài đọc \"{currentPassage.Title}\" thiếu [END PASSAGE]."));
            }
            if (currentQuestion != null)
            {
                preview.Errors.Add(Issue("question.end.missing", $"Câu {currentQuestion.QuestionNumber} thiếu [END QUESTION]."));
            }
            if (!hasEndExam)
            {
                preview.HasEndExam = false;
            }

            preview.HasEndExam = hasEndExam;
            preview.Errors.AddRange(Validate(preview).Where(issue => !issue.QuestionNumber.HasValue));
            foreach (var question in preview.Questions)
            {
                question.Errors.AddRange(ValidateQuestion(preview, question));
            }

            return preview;
        }

        private static List<WordImportIssueDto> ValidateQuestion(
            WordExamPreviewDto preview,
            WordQuestionDto question)
        {
            var questionPreview = new WordExamPreviewDto
            {
                Title = preview.Title,
                Description = preview.Description,
                DurationMinutes = preview.DurationMinutes,
                HasEndExam = preview.HasEndExam,
                ReadingPassages = preview.ReadingPassages,
                Questions = new List<WordQuestionDto> { question }
            };
            return new WordExamImportParser().Validate(questionPreview)
                .Where(issue => issue.QuestionNumber.HasValue)
                .ToList();
        }

        private static void AppendContinuation(
            string section,
            string line,
            WordExamPreviewDto preview,
            WordReadingPassageDto passage,
            WordQuestionDto question)
        {
            if (section == "passage" && passage != null)
            {
                passage.Content = AppendLine(passage.Content, line);
            }
            else if (question != null)
            {
                var option = question.Options.LastOrDefault();
                if (option != null && section == "options")
                {
                    option.Content = AppendLine(option.Content, line);
                }
                else if (section == "explanation")
                {
                    question.Explanation = AppendLine(question.Explanation, line);
                }
                else if (section == "hint")
                {
                    question.Hint = AppendLine(question.Hint, line);
                }
                else if (section == "answer")
                {
                    question.Answer = AppendLine(question.Answer, line);
                }
                else
                {
                    question.QuestionText = AppendLine(question.QuestionText, line);
                    section = "question";
                }
            }
            else if (section == "description")
            {
                preview.Description = AppendLine(preview.Description, line);
            }
            else if (section == "root" && !string.IsNullOrWhiteSpace(line))
            {
                preview.Errors.Add(Issue(
                    "document.text.unexpected",
                    $"Có nội dung nằm ngoài các thẻ đề thi: {line}"));
            }
        }

        private static void AddInlineOptions(
            string text,
            ICollection<WordQuestionOptionDto> options)
        {
            var matches = InlineOptionRegex.Matches(text ?? string.Empty);
            for (var index = 0; index < matches.Count; index++)
            {
                var match = matches[index];
                var contentStart = match.Index + match.Length;
                var contentEnd = index + 1 < matches.Count
                    ? matches[index + 1].Index
                    : text.Length;
                options.Add(new WordQuestionOptionDto
                {
                    Label = GetOptionLabel(match),
                    Content = text.Substring(contentStart, contentEnd - contentStart).Trim()
                });
            }
        }

        private static List<WordQuestionOptionDto> GetOptions(
            int questionNumber,
            IDictionary<int, WordQuestionDto> questionsByNumber,
            IDictionary<int, List<WordQuestionOptionDto>> pendingOptionsByQuestionNumber)
        {
            if (questionsByNumber.TryGetValue(questionNumber, out var question))
            {
                return question.Options;
            }

            if (!pendingOptionsByQuestionNumber.TryGetValue(questionNumber, out var pendingOptions))
            {
                pendingOptions = new List<WordQuestionOptionDto>();
                pendingOptionsByQuestionNumber.Add(questionNumber, pendingOptions);
            }

            return pendingOptions;
        }

        private static void AppendToLastOption(
            IList<WordQuestionOptionDto> options,
            string line)
        {
            var lastOption = options.LastOrDefault();
            if (lastOption != null)
            {
                lastOption.Content = AppendLine(lastOption.Content, line);
            }
        }

        private static string AppendLine(string existing, string value)
        {
            return string.IsNullOrEmpty(existing)
                ? value
                : existing + Environment.NewLine + value;
        }

        private static string GetOptionLabel(Match match)
        {
            return (match.Groups["parenthesized"].Success
                ? match.Groups["parenthesized"].Value
                : match.Groups["plain"].Value).ToUpperInvariant();
        }

        private static bool IsSupportedOptionLabel(string label)
        {
            var normalized = (label ?? string.Empty).ToUpperInvariant();
            return normalized == "A" || normalized == "B" || normalized == "C" || normalized == "D";
        }

        private static Dictionary<string, string> ParseAttributes(string value)
        {
            var attributes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (Match match in AttributeRegex.Matches(value ?? string.Empty))
            {
                attributes[match.Groups["name"].Value.ToUpperInvariant()] =
                    match.Groups["quoted"].Success
                        ? match.Groups["quoted"].Value
                        : match.Groups["plain"].Value;
            }
            return attributes;
        }

        private static string GetAttribute(IDictionary<string, string> attributes, string name)
        {
            return attributes.TryGetValue(name, out var value) ? value.Trim() : string.Empty;
        }

        private static int ParseIntegerAttribute(IDictionary<string, string> attributes, string name)
        {
            var value = GetAttribute(attributes, name);
            return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var number)
                ? number
                : 0;
        }

        private static WordImportIssueDto Issue(string code, string message, int? questionNumber = null)
        {
            return new WordImportIssueDto
            {
                Code = code,
                Message = message,
                QuestionNumber = questionNumber
            };
        }
    }

    public class WordExamImportException : Exception
    {
        public WordExamImportException(string message)
            : base(message)
        {
        }
    }
}
