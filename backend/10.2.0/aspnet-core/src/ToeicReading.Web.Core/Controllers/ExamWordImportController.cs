using Abp.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Threading.Tasks;
using ToeicReading.Authorization;
using ToeicReading.Controllers;
using ToeicReading.Exams;
using ToeicReading.Exams.Dto;

namespace ToeicReading.Controllers
{
    /// <summary>Admin endpoints for parsing and importing TOEIC exams from Word.</summary>
    [Route("api/admin/exams")]
    public class ExamWordImportController : ToeicReadingControllerBase
    {
        private const long MaxRequestBytes = 11 * 1024 * 1024;

        private readonly IWordExamImportService _importService;

        /// <summary>Creates the controller with the exam import service.</summary>
        public ExamWordImportController(IWordExamImportService importService)
        {
            _importService = importService;
        }

        /// <summary>Returns all exams available to administrators, including drafts.</summary>
        [AbpMvcAuthorize(PermissionNames.Pages_Exams_Import)]
        [HttpGet]
        public async Task<IActionResult> GetExams()
        {
            return Ok(new { result = await _importService.GetAdminExamsAsync() });
        }

        /// <summary>Returns an exam including answer keys for authorized administrators.</summary>
        [AbpMvcAuthorize(PermissionNames.Pages_Exams_Import)]
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetExamDetail(int id)
        {
            var exam = await _importService.GetAdminExamDetailAsync(id);
            if (exam == null)
            {
                return NotFound(new { error = "Không tìm thấy đề thi." });
            }

            return Ok(new { result = exam });
        }

        /// <summary>Updates an unpublished exam and its reading content atomically.</summary>
        [AbpMvcAuthorize(PermissionNames.Pages_Exams_Import)]
        [HttpPut("{id:int}")]
        [Consumes("application/json")]
        public async Task<IActionResult> UpdateExam(int id, [FromBody] UpdateAdminExamInputDto input)
        {
            var updated = await _importService.UpdateAdminExamAsync(id, input);
            if (!updated)
            {
                return NotFound(new { error = "Không tìm thấy đề thi." });
            }

            return Ok(new { result = true });
        }

        /// <summary>Parses a DOCX file without persisting exam data.</summary>
        [WordImportAuthorize]
        [HttpPost("parse-word")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(MaxRequestBytes)]
        [RequestFormLimits(MultipartBodyLengthLimit = MaxRequestBytes)]
        public async Task<IActionResult> ParseWord(IFormFile file)
        {
            if (file == null)
            {
                return BadRequest(new { error = "Vui lòng chọn tệp DOCX." });
            }
            if (file.Length == 0)
            {
                return BadRequest(new { error = "Tệp DOCX rỗng." });
            }
            if (file.Length > WordExamImportParser.MaxFileSizeBytes)
            {
                return BadRequest(new { error = "Tệp DOCX vượt quá giới hạn 10 MB." });
            }
            if (!string.Equals(Path.GetExtension(file.FileName), ".docx", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new { error = "Chỉ chấp nhận tệp Word có định dạng .docx." });
            }

            try
            {
                using (var buffer = new MemoryStream((int)file.Length))
                {
                    await file.CopyToAsync(buffer);
                    var preview = await _importService.ParseWordAsync(file.FileName, buffer.ToArray());
                    return Ok(new { result = preview });
                }
            }
            catch (WordExamImportException exception)
            {
                return BadRequest(new { error = exception.Message });
            }
        }

        /// <summary>Persists a validated exam preview as an unpublished exam.</summary>
        [WordImportAuthorize]
        [HttpPost("import-word")]
        [Consumes("application/json")]
        [RequestSizeLimit(MaxRequestBytes)]
        public async Task<IActionResult> ImportWord([FromBody] WordExamPreviewDto preview)
        {
            try
            {
                var result = await _importService.ImportWordAsync(preview);
                return Ok(new { result });
            }
            catch (WordExamValidationException exception)
            {
                return BadRequest(new
                {
                    error = exception.Message,
                    errors = exception.Errors
                });
            }
            catch (WordExamDuplicateException exception)
            {
                return Conflict(new { error = exception.Message });
            }
        }
    }
}
