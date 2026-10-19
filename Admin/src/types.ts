export type PublicationStatus = 'Đã xuất bản' | 'Bản nháp';

export interface Exam {
  id: string;
  title: string;
  duration: number;
  questionCount: number;
  updatedAt: string;
  status: PublicationStatus;
  description: string;
}

export interface Question {
  id: string;
  part: string;
  passageTitle: string;
  passage: string;
  prompt: string;
  options: Record<'A' | 'B' | 'C' | 'D', string>;
  correctAnswer: '' | 'A' | 'B' | 'C' | 'D';
  hint: string;
  explanation: string;
}

export interface DashboardSummary {
  examCount: number;
  questionCount: number;
  attemptCount: number;
  completionRate: number;
}

export interface Activity {
  title: string;
  detail: string;
  time: string;
  tone: 'blue' | 'green' | 'violet';
}

export interface ExamDraft {
  title: string;
  duration: number;
  description: string;
}

export interface AdminExamDetail extends Exam {
  readingPassages: Array<{
    id: number;
    partNumber: number;
    title: string;
    content: string;
    orderNumber: number;
  }>;
  questions: Array<{
    id: number;
    readingPassageId: number | null;
    partNumber: number;
    questionNumber: number;
    questionText: string;
    hint: string;
    explanation: string;
    options: Array<{
      id: number;
      label: string;
      content: string;
      orderNumber: number;
      isCorrect: boolean;
    }>;
  }>;
}

export interface QuestionDraft extends Omit<Question, 'id'> {
  id?: string;
}

export interface WordImportIssue {
  code: string;
  message: string;
  questionNumber: number | null;
}

export interface WordQuestionOption {
  label: string;
  content: string;
}

export interface WordQuestionPreview {
  questionNumber: number;
  partNumber: number;
  readingPassageTempId: string | null;
  questionText: string;
  options: WordQuestionOption[];
  answer: string;
  explanation: string;
  hint: string;
  errors: WordImportIssue[];
}

export interface WordReadingPassagePreview {
  tempId: string;
  partNumber: number;
  title: string;
  content: string;
  orderNumber: number;
}

export interface WordExamPreview {
  title: string;
  description: string;
  durationMinutes: number;
  hasEndExam: boolean;
  readingPassages: WordReadingPassagePreview[];
  questions: WordQuestionPreview[];
  errors: WordImportIssue[];
  warnings: WordImportIssue[];
  isValid: boolean;
}

export interface WordExamImportResult {
  examId: number;
  passageCount: number;
  questionCount: number;
  message: string;
}
