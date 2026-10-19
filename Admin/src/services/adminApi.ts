
import {
  sampleActivities,
  sampleDashboard,
  sampleExams,
  sampleQuestions,
} from '../data/sampleData';

import type {
  Activity,
  AdminExamDetail,
  DashboardSummary,
  Exam,
  ExamDraft,
  Question,
  QuestionDraft,
  WordExamImportResult,
  WordExamPreview,
} from '../types';

import { ApiClient } from './apiClient';

export const usingSampleData =
  import.meta.env.VITE_USE_MOCK_DATA !== 'false';

type AbpResponse<T> = {
  result: T;
};

type AdminExamDetailDto = {
  id: number;
  title: string;
  description?: string | null;
  durationMinutes: number;
  creationTime: string;
  isPublished: boolean;
  readingPassages?: Array<{
    id: number;
    partNumber: number;
    title?: string | null;
    content?: string | null;
    orderNumber: number;
  }> | null;
  questions?: Array<{
    id: number;
    readingPassageId: number | null;
    partNumber: number;
    questionNumber: number;
    questionText?: string | null;
    hint?: string | null;
    explanation?: string | null;
    options?: Array<{
      id: number;
      label?: string | null;
      content?: string | null;
      orderNumber: number;
      isCorrect: boolean;
    }> | null;
  }> | null;
};

function unwrapResult<T>(response: T | AbpResponse<T>): T {
  let value: unknown = response;
  while (typeof value === 'object' && value !== null && 'result' in value) {
    value = (value as { result: unknown }).result;
  }

  return value as T;
}

export class AdminApiService {
  private readonly examDetailRequests = new Map<string, Promise<AdminExamDetail>>();

  constructor(private readonly client = new ApiClient()) {}

  isAuthenticated(): boolean {
    const token = localStorage.getItem('Abp.AuthToken');
    if (!token) {
      return false;
    }

    const storedExpiry = Number(localStorage.getItem('Abp.AuthTokenExpiresAt'));
    const expiresAt = Number.isFinite(storedExpiry) && storedExpiry > 0
      ? storedExpiry
      : getTokenExpiration(token);
    if (expiresAt !== null && expiresAt <= Date.now()) {
      this.signOut();
      return false;
    }

    return true;
  }

  async authenticate(userNameOrEmailAddress: string, password: string): Promise<void> {
    type AuthenticateResult = { accessToken: string; expireInSeconds?: number };
    const response = await this.client.post<
      AuthenticateResult | AbpResponse<AuthenticateResult>
    >('/api/TokenAuth/Authenticate', {
      userNameOrEmailAddress,
      password,
      rememberClient: true,
    });
    const result = unwrapResult(response);
    if (!result.accessToken) {
      throw new Error('Backend không trả về token đăng nhập.');
    }
    localStorage.setItem('Abp.AuthToken', result.accessToken);
    const expiresAt = typeof result.expireInSeconds === 'number'
      ? Date.now() + result.expireInSeconds * 1000
      : getTokenExpiration(result.accessToken);
    if (expiresAt !== null) {
      localStorage.setItem('Abp.AuthTokenExpiresAt', String(expiresAt));
    } else {
      localStorage.removeItem('Abp.AuthTokenExpiresAt');
    }
    this.examDetailRequests.clear();
  }

  signOut(): void {
    localStorage.removeItem('Abp.AuthToken');
    localStorage.removeItem('Abp.AuthTokenExpiresAt');
    this.examDetailRequests.clear();
  }

  // =========================================================
  // 1. DASHBOARD
  // =========================================================
  async getDashboard(exams?: Exam[]): Promise<DashboardSummary> {
    if (usingSampleData) {
      return sampleDashboard;
    }

    const currentExams = exams ?? await this.getExams();

    return {
      examCount: currentExams.length,
      questionCount: currentExams.reduce((count, exam) => count + exam.questionCount, 0),
      attemptCount: 0,
      completionRate: 0,
    };
  }

  // =========================================================
  // 2. HOẠT ĐỘNG GẦN ĐÂY
  // =========================================================
  async getActivities(): Promise<Activity[]> {
    return usingSampleData ? sampleActivities : [];
  }

  // =========================================================
  // 3. DANH SÁCH ĐỀ THI
  // GET /api/admin/exams (requires the Admin exam permission)
  // =========================================================
  async getExams(): Promise<Exam[]> {
    if (usingSampleData) {
      return sampleExams;
    }

    type BackendExamDto = {
      id: number;
      title: string;
      description?: string | null;
      durationMinutes: number;
      questionCount: number;
      creationTime: string;
      isPublished: boolean;
    };

    const response = await this.client.get<
      BackendExamDto[] | AbpResponse<BackendExamDto[]>
    >('/api/admin/exams');

    const exams = unwrapResult(response);

    return exams.map((exam) => ({
      id: String(exam.id),
      title: exam.title,
      description: exam.description ?? '',
      duration: exam.durationMinutes,
      questionCount: exam.questionCount,
      updatedAt: Number.isNaN(Date.parse(exam.creationTime))
        ? 'Chưa có thông tin'
        : new Date(exam.creationTime).toLocaleDateString('vi-VN'),
      status: exam.isPublished ? 'Đã xuất bản' as const : 'Bản nháp' as const,
    }));
  }

  async getAdminExamDetail(id: string): Promise<AdminExamDetail> {
    const existingRequest = this.examDetailRequests.get(id);
    if (existingRequest) {
      return existingRequest;
    }

    const request = this.client.get<
      AdminExamDetailDto | AbpResponse<AdminExamDetailDto>
    >(`/api/admin/exams/${encodeURIComponent(id)}`)
      .then(unwrapResult)
      .then((detail) => ({
        id: String(detail.id),
        title: detail.title ?? '',
        description: detail.description ?? '',
        duration: detail.durationMinutes,
        questionCount: detail.questions?.length ?? 0,
        updatedAt: Number.isNaN(Date.parse(detail.creationTime))
          ? 'Chưa có thông tin'
          : new Date(detail.creationTime).toLocaleDateString('vi-VN'),
        status: detail.isPublished ? 'Đã xuất bản' as const : 'Bản nháp' as const,
        readingPassages: (detail.readingPassages ?? []).map((passage) => ({
          ...passage,
          title: passage.title ?? '',
          content: passage.content ?? '',
        })),
        questions: (detail.questions ?? []).map((question) => ({
          ...question,
          questionText: question.questionText ?? '',
          hint: question.hint ?? '',
          explanation: question.explanation ?? '',
          options: (question.options ?? []).map((option) => ({
            ...option,
            label: option.label ?? '',
            content: option.content ?? '',
          })),
        })),
      }));
    let cachedRequest: Promise<AdminExamDetail>;
    cachedRequest = request.catch((error: unknown) => {
      if (this.examDetailRequests.get(id) === cachedRequest) {
        this.examDetailRequests.delete(id);
      }
      throw error;
    });
    this.examDetailRequests.set(id, cachedRequest);
    return cachedRequest;
  }

  async getAdminExamDetails(exams?: Exam[]): Promise<AdminExamDetail[]> {
    if (usingSampleData) {
      return [];
    }
    const currentExams = exams ?? await this.getExams();
    return Promise.all(currentExams.map((exam) => this.getAdminExamDetail(exam.id)));
  }

  // =========================================================
  // 4. TẠO ĐỀ THI
  // Lưu ý: endpoint này cần API backend tương ứng.
  // =========================================================
  async createExam(payload: ExamDraft): Promise<Exam> {
    if (usingSampleData) {
      return {
        ...payload,
        id: `DEMO-${Date.now()}`,
        questionCount: 0,
        updatedAt: new Date().toLocaleDateString('vi-VN'),
        status: 'Bản nháp',
      };
    }

    throw new Error(
      'Backend hiện chưa có API tạo đề thi thủ công. Hãy dùng chức năng Tải đề từ Word để tạo đề.'
    );
  }

  // =========================================================
  // 5. CẬP NHẬT ĐỀ THI
  // PUT /api/services/app/Exam/UpdateExam?id=1
  // =========================================================
  async updateExam(
    id: string,
    payload: ExamDraft,
    previous?: Exam
  ): Promise<Exam> {
    if (usingSampleData) {
      const current =
        previous ?? sampleExams.find((exam) => exam.id === id);

      return {
        ...payload,
        id,
        questionCount: current?.questionCount ?? 0,
        updatedAt: new Date().toLocaleDateString('vi-VN'),
        status: current?.status ?? 'Bản nháp',
      };
    }

    const examId = Number(id);

    if (!Number.isInteger(examId) || examId <= 0) {
      throw new Error('Mã đề thi không hợp lệ.');
    }

    const status = previous?.status ?? 'Bản nháp';

    await this.client.put<void>(
      `/api/services/app/Exam/UpdateExam?id=${examId}`,
      {
        title: payload.title,
        description: payload.description ?? '',
        durationMinutes: payload.duration,
        isPublished: status === 'Đã xuất bản',
      }
    );
    this.examDetailRequests.delete(id);

    return {
      ...payload,
      id,
      questionCount: previous?.questionCount ?? 0,
      updatedAt: new Date().toLocaleDateString('vi-VN'),
      status,
    };
  }

  async updateAdminExam(id: string, draft: AdminExamDetail): Promise<AdminExamDetail> {
    if (usingSampleData) {
      throw new Error('Không thể lưu đề thi thật khi đang bật dữ liệu mẫu.');
    }

    const examId = Number(id);
    if (!Number.isInteger(examId) || examId <= 0 || Number(draft.id) !== examId) {
      throw new Error('Mã đề thi không hợp lệ.');
    }

    const response = await this.client.put<boolean | AbpResponse<boolean>>(
      `/api/admin/exams/${examId}`,
      {
        title: draft.title,
        description: draft.description,
        durationMinutes: draft.duration,
        readingPassages: draft.readingPassages.map((passage) => ({
          id: passage.id > 0 ? passage.id : 0,
          clientKey: passage.id < 0 ? String(passage.id) : null,
          partNumber: passage.partNumber,
          title: passage.title,
          content: passage.content,
          orderNumber: passage.orderNumber,
        })),
        questions: draft.questions.map((question) => ({
          id: question.id > 0 ? question.id : 0,
          partNumber: question.partNumber,
          questionNumber: question.questionNumber,
          readingPassageId:
            question.readingPassageId && question.readingPassageId > 0
              ? question.readingPassageId
              : null,
          readingPassageClientKey:
            question.readingPassageId && question.readingPassageId < 0
              ? String(question.readingPassageId)
              : null,
          questionText: question.questionText,
          hint: question.hint,
          explanation: question.explanation,
          options: question.options.map((option) => ({
            id: option.id > 0 ? option.id : 0,
            label: option.label,
            content: option.content,
            orderNumber: option.orderNumber,
            isCorrect: option.isCorrect,
          })),
        })),
      },
    );
    if (!unwrapResult(response)) {
      throw new Error('Backend không xác nhận đã lưu đề thi.');
    }

    this.examDetailRequests.delete(id);
    return this.getAdminExamDetail(id);
  }

  async setExamPublicationStatus(id: string, isPublished: boolean): Promise<boolean> {
    if (usingSampleData) {
      throw new Error(
        'Không thể thay đổi trạng thái khi đang dùng dữ liệu mẫu. Kết nối backend thật để lưu thay đổi.'
      );
    }

    const examId = Number(id);
    if (!Number.isInteger(examId) || examId <= 0) {
      throw new Error('Mã đề thi không hợp lệ.');
    }

    const response = await this.client.post<
      boolean | AbpResponse<boolean>
    >('/api/services/app/Exam/SetPublicationStatus', {
      id: examId,
      isPublished,
    });
    const savedStatus = unwrapResult(response);
    if (savedStatus !== isPublished) {
      throw new Error('Backend chưa xác nhận trạng thái kích hoạt mới.');
    }

    this.examDetailRequests.delete(id);
    return savedStatus;
  }

  // =========================================================
  // 6. LẤY CÂU HỎI
  // GET /api/services/app/Exam/GetExamDetail?id=1
  // =========================================================
  async getQuestions(exams?: Exam[]): Promise<Question[]> {
    if (usingSampleData) {
      return sampleQuestions;
    }

    const details = await this.getAdminExamDetails(exams);
    const allQuestions: Question[] = [];

    for (const detail of details) {
      for (const question of detail.questions) {
        const options: Record<'A' | 'B' | 'C' | 'D', string> = {
          A: '',
          B: '',
          C: '',
          D: '',
        };

        for (const option of question.options) {
          const label = option.label.toUpperCase();

          if (
            label === 'A' ||
            label === 'B' ||
            label === 'C' ||
            label === 'D'
          ) {
            options[label] = option.content;
          }
        }

        const passage = detail.readingPassages.find(
          (item) => item.id === question.readingPassageId,
        );
        const correctLabel = question.options
          .find((option) => option.isCorrect)
          ?.label.toUpperCase();

        allQuestions.push({
          id: String(question.id),
          part: `Part ${question.partNumber}`,
          passageTitle: passage?.title ?? '',
          passage: passage?.content ?? '',
          prompt: question.questionText,
          options,
          correctAnswer:
            correctLabel === 'A' ||
            correctLabel === 'B' ||
            correctLabel === 'C' ||
            correctLabel === 'D'
              ? correctLabel
              : '',
          hint: question.hint ?? '',
          explanation: question.explanation ?? '',
        });
      }
    }

    return allQuestions;
  }

  // =========================================================
  // 7. TẠO CÂU HỎI
  // POST /api/services/app/Exam/CreateQuestion
  // =========================================================
  async createQuestion(
    payload: QuestionDraft
  ): Promise<Question> {
    if (usingSampleData) {
      return {
        ...payload,
        id: `DEMO-Q-${Date.now()}`,
      };
    }

    const partMatch = payload.part.match(/\d+/);
    const partNumber = partMatch ? Number(partMatch[0]) : 0;

    if (![5, 6, 7].includes(partNumber)) {
      throw new Error('Phần thi phải là Part 5, Part 6 hoặc Part 7.');
    }

    if (!payload.prompt.trim()) {
      throw new Error('Vui lòng nhập nội dung câu hỏi.');
    }

    const labels = ['A', 'B', 'C', 'D'] as const;
    const correctAnswer = payload.correctAnswer.toUpperCase();

    if (!labels.includes(correctAnswer as typeof labels[number])) {
      throw new Error('Đáp án đúng phải là A, B, C hoặc D.');
    }

    const options = labels.map((label, index) => ({
      label,
      content: payload.options[label].trim(),
      isCorrect: label === correctAnswer,
      orderNumber: index + 1,
    }));

    if (options.some((option) => !option.content)) {
      throw new Error('Vui lòng nhập đầy đủ 4 phương án A, B, C, D.');
    }

    const exams = await this.getExams();

    if (exams.length === 0) {
      throw new Error(
        'Không có đề thi đã công bố. Hãy tạo hoặc công bố đề thi trước.'
      );
    }

    // Tạm dùng đề thi đầu tiên.
    // Nên bổ sung trường chọn đề thi trong biểu mẫu quản trị.
    const examId = Number(exams[0].id);

    if (!Number.isInteger(examId) || examId <= 0) {
      throw new Error('Mã đề thi không hợp lệ.');
    }

    const examDetail = await this.getAdminExamDetail(String(examId));
    let readingPassageId: number | null = null;

    if (partNumber === 6 || partNumber === 7) {
      const passage = examDetail.readingPassages.find(
        (item) =>
          item.partNumber === partNumber &&
          (
            item.title.trim() === (payload.passageTitle ?? '').trim() ||
            item.content.trim() === (payload.passage ?? '').trim()
          )
      );

      if (!passage) {
        throw new Error(
          `Không tìm thấy bài đọc Part ${partNumber} trong đề thi. Hãy kiểm tra hoặc tạo bài đọc trước.`
        );
      }

      readingPassageId = passage.id;
    }

    const questionNumber =
      Math.max(0, ...examDetail.questions.map((q) => q.questionNumber)) + 1;

    const result = await this.client.post<
      number | AbpResponse<number>
    >(
      '/api/services/app/Exam/CreateQuestion',
      {
        examId,
        readingPassageId,
        partNumber,
        questionNumber,
        questionText: payload.prompt.trim(),
        hint: payload.hint ?? '',
        explanation: payload.explanation ?? '',
        shuffleOptions: false,
        options,
      }
    );

    const questionId = unwrapResult(result);

    if (!questionId) {
      throw new Error('Backend không trả về mã câu hỏi đã tạo.');
    }
    this.examDetailRequests.delete(String(examId));

    return {
      ...payload,
      id: String(questionId),
    };
  }

  // =========================================================
  // 8. CẬP NHẬT CÂU HỎI
  // Endpoint này cần API backend tương ứng.
  // =========================================================
  async updateQuestion(
    id: string,
    payload: QuestionDraft
  ): Promise<Question> {
    if (usingSampleData) {
      return {
        ...payload,
        id,
      };
    }

    void id;
    void payload;
    throw new Error(
      'Backend hiện chưa có API cập nhật câu hỏi. Không có dữ liệu nào được gửi hoặc thay đổi.'
    );
  }

  async parseExamDocument(file: File): Promise<WordExamPreview> {
    if (usingSampleData) {
      throw new Error(
        'Chế độ dữ liệu mẫu không hỗ trợ phân tích hoặc lưu đề thật. Hãy đặt VITE_USE_MOCK_DATA=false để dùng API backend.'
      );
    }

    const formData = new FormData();
    formData.append('file', file);

    const response = await this.client.post<
      WordExamPreview | AbpResponse<WordExamPreview>
    >(
      '/api/admin/exams/parse-word',
      formData
    );
    return unwrapResult(response);
  }

  async saveImportedExam(
    preview: WordExamPreview
  ): Promise<WordExamImportResult> {
    if (usingSampleData) {
      throw new Error(
        'Không thể lưu đề vào SQL Server khi VITE_USE_MOCK_DATA đang bật. Hãy chuyển sang API thật.'
      );
    }

    const response = await this.client.post<
      WordExamImportResult | AbpResponse<WordExamImportResult>
    >('/api/admin/exams/import-word', preview);
    const result = unwrapResult(response);
    this.examDetailRequests.clear();
    return result;
  }
}

function getTokenExpiration(token: string): number | null {
  try {
    const payload = token.split('.')[1];
    if (!payload) {
      return null;
    }

    const base64 = payload.replace(/-/g, '+').replace(/_/g, '/');
    const padded = base64.padEnd(Math.ceil(base64.length / 4) * 4, '=');
    const decoded = Uint8Array.from(atob(padded), (character) =>
      character.charCodeAt(0),
    );
    const claims = JSON.parse(new TextDecoder().decode(decoded)) as {
      exp?: unknown;
    };
    return typeof claims.exp === 'number' ? claims.exp * 1000 : null;
  } catch {
    return null;
  }
}
