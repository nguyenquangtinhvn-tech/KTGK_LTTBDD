import type { Activity, DashboardSummary, Exam, Question } from '../types';

export const sampleDashboard: DashboardSummary = {
  examCount: 42,
  questionCount: 1280,
  attemptCount: 8642,
  completionRate: 78.4,
};

export const sampleExams: Exam[] = [
  {
    id: 'TR-2025-001',
    title: 'TOEIC Reading - Practice Test 01',
    duration: 75,
    questionCount: 100,
    updatedAt: '18/10/2025',
    status: 'Đã xuất bản',
    description: 'Đề luyện tập đầy đủ các Part 5, 6 và 7.',
  },
  {
    id: 'TR-2025-002',
    title: 'Business Communication Essentials',
    duration: 60,
    questionCount: 75,
    updatedAt: '16/10/2025',
    status: 'Đã xuất bản',
    description: 'Chủ đề giao tiếp và thư tín trong môi trường công sở.',
  },
  {
    id: 'TR-2025-003',
    title: 'Travel & Hospitality',
    duration: 45,
    questionCount: 50,
    updatedAt: '14/10/2025',
    status: 'Bản nháp',
    description: 'Bài đọc về đặt phòng, lịch trình và dịch vụ du lịch.',
  },
  {
    id: 'TR-2025-004',
    title: 'Workplace Announcements',
    duration: 50,
    questionCount: 60,
    updatedAt: '12/10/2025',
    status: 'Đã xuất bản',
    description: 'Thông báo và hướng dẫn thường gặp tại nơi làm việc.',
  },
  {
    id: 'TR-2025-005',
    title: 'Technology in the Office',
    duration: 55,
    questionCount: 70,
    updatedAt: '10/10/2025',
    status: 'Bản nháp',
    description: 'Từ vựng và nội dung đọc về công nghệ văn phòng.',
  },
];

export const sampleQuestions: Question[] = [
  {
    id: 'Q-0071',
    part: 'Part 5',
    passageTitle: 'Câu hỏi ngữ pháp độc lập',
    passage:
      'The marketing team will review the campaign results during Friday’s planning meeting.',
    prompt: 'All applicants must submit their forms _____ the end of the month.',
    options: {
      A: 'by',
      B: 'until',
      C: 'among',
      D: 'during',
    },
    correctAnswer: 'A',
    hint: 'Chọn giới từ diễn tả hạn chót.',
    explanation:
      '“By the end of the month” có nghĩa là không muộn hơn cuối tháng, phù hợp với một thời hạn.',
  },
  {
    id: 'Q-0072',
    part: 'Part 6',
    passageTitle: 'Thông báo thay đổi lịch làm việc',
    passage:
      'The office will close at 3 P.M. on Friday for scheduled maintenance. Employees should save their work before leaving.',
    prompt: 'What should employees do before leaving the office?',
    options: {
      A: 'Attend a planning meeting',
      B: 'Save their work',
      C: 'Contact the maintenance team',
      D: 'Change their work schedule',
    },
    correctAnswer: 'B',
    hint: 'Tìm câu trong thông báo có từ “before leaving”.',
    explanation:
      'Thông báo yêu cầu nhân viên lưu công việc trước khi rời văn phòng.',
  },
  {
    id: 'Q-0073',
    part: 'Part 7',
    passageTitle: 'Email mời tham dự hội thảo',
    passage:
      'To: Department Managers\nFrom: Training Office\nSubject: Customer Service Workshop\n\nPlease register your team by November 12. The workshop will be held in Conference Room B.',
    prompt: 'By what date should managers register their teams?',
    options: {
      A: 'November 2',
      B: 'November 10',
      C: 'November 12',
      D: 'November 21',
    },
    correctAnswer: 'C',
    hint: 'Chú ý ngày được dùng cùng động từ “register”.',
    explanation:
      'Email ghi rõ các quản lý cần đăng ký nhóm trước hoặc trong ngày 12 tháng 11.',
  },
  {
    id: 'Q-0074',
    part: 'Part 5',
    passageTitle: 'Câu hỏi từ vựng độc lập',
    passage:
      'The new inventory system gives staff an accurate overview of current stock.',
    prompt: 'The updated schedule is available _____ the company website.',
    options: {
      A: 'in',
      B: 'on',
      C: 'at',
      D: 'to',
    },
    correctAnswer: 'B',
    hint: 'Xét cách dùng giới từ với “website”.',
    explanation:
      'Trong tiếng Anh, ta thường nói “available on the website”.',
  },
];

export const sampleActivities: Activity[] = [
  {
    title: 'Đề thi mới được xuất bản',
    detail: 'TOEIC Reading - Practice Test 01',
    time: '10 phút trước',
    tone: 'blue',
  },
  {
    title: 'Đã thêm câu hỏi mới',
    detail: 'Bài đọc: Customer Service Workshop',
    time: '1 giờ trước',
    tone: 'green',
  },
  {
    title: 'Đề thi đang ở trạng thái bản nháp',
    detail: 'Travel & Hospitality',
    time: 'Hôm qua',
    tone: 'violet',
  },
];
