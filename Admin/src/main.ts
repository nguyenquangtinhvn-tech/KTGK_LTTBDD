import './styles.css';
import { sampleActivities, sampleDashboard } from './data/sampleData';
import { AdminApiService, usingSampleData } from './services/adminApi';
import { ApiClientError } from './services/apiClient';
import type {
  Activity,
  AdminExamDetail,
  DashboardSummary,
  Exam,
  Question,
  WordExamImportResult,
  WordExamPreview,
} from './types';

type Page = 'overview' | 'exams' | 'upload' | 'questions' | 'exam-detail';
type ExamStatusFilter = 'all' | 'Đã xuất bản' | 'Bản nháp';
type UploadStatus = 'empty' | 'ready' | 'processing' | 'complete' | 'importing' | 'imported' | 'error';

const api = new AdminApiService();
const root = document.querySelector<HTMLDivElement>('#app')!;
const state: {
  page: Page;
  loading: boolean;
  error: string;
  dashboard: DashboardSummary;
  activities: Activity[];
  exams: Exam[];
  examDetails: Record<string, AdminExamDetail>;
  questions: Question[];
  requiresAuth: boolean;
  accessDenied: boolean;
  loginError: string;
  examSearch: string;
  examStatus: ExamStatusFilter;
  examPage: number;
  questionSearch: string;
  questionPart: string;
  questionPage: number;
  selectedQuestionId: string;
  newQuestionId: string;
  modalOpen: boolean;
  editingExamId: string;
  examEditDraft: AdminExamDetail | null;
  examEditOriginal: string;
  examEditPart: 5 | 6 | 7;
  examEditSaving: boolean;
  publicationUpdatingExamId: string;
  uploadFile: File | null;
  uploadStatus: UploadStatus;
  uploadPreview: WordExamPreview | null;
  uploadResult: WordExamImportResult | null;
  uploadError: string;
  uploadPreviewOpen: boolean;
  mobileMenuOpen: boolean;
} = {
  page: 'overview',
  loading: true,
  error: '',
  dashboard: usingSampleData ? sampleDashboard : {
    examCount: 0,
    questionCount: 0,
    attemptCount: 0,
    completionRate: 0,
  },
  activities: sampleActivities,
  exams: [],
  examDetails: {},
  questions: [],
  requiresAuth: false,
  accessDenied: false,
  loginError: '',
  examSearch: '',
  examStatus: 'all',
  examPage: 1,
  questionSearch: '',
  questionPart: 'all',
  questionPage: 1,
  selectedQuestionId: '',
  newQuestionId: '',
  modalOpen: false,
  editingExamId: '',
  examEditDraft: null,
  examEditOriginal: '',
  examEditPart: 5,
  examEditSaving: false,
  publicationUpdatingExamId: '',
  uploadFile: null,
  uploadStatus: 'empty',
  uploadPreview: null,
  uploadResult: null,
  uploadError: '',
  uploadPreviewOpen: false,
  mobileMenuOpen: false,
};

const pageTitles: Record<Page, string> = {
  overview: 'Tổng quan',
  exams: 'Quản lý đề thi',
  upload: 'Tải đề từ Word',
  questions: 'Quản lý câu hỏi',
  'exam-detail': 'Chi tiết đề thi',
};

const navItems: Array<{ page: Page; label: string; iconName: string; href: string }> = [
  { page: 'overview', label: 'Tổng quan', iconName: 'grid', href: '#/overview' },
  { page: 'exams', label: 'Quản lý đề thi', iconName: 'file', href: '#/exams' },
  { page: 'upload', label: 'Tải đề từ Word', iconName: 'upload', href: '#/upload' },
  { page: 'questions', label: 'Quản lý câu hỏi', iconName: 'list', href: '#/questions' },
];

const iconPaths: Record<string, string> = {
  grid: '<rect x="3" y="3" width="7" height="7" rx="1.5"/><rect x="14" y="3" width="7" height="7" rx="1.5"/><rect x="3" y="14" width="7" height="7" rx="1.5"/><rect x="14" y="14" width="7" height="7" rx="1.5"/>',
  file: '<path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"/><path d="M14 2v6h6M8 13h8M8 17h8"/>',
  upload: '<path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4"/><path d="m17 8-5-5-5 5M12 3v12"/>',
  list: '<path d="M9 6h11M9 12h11M9 18h11"/><path d="M4 6h.01M4 12h.01M4 18h.01"/>',
  search: '<circle cx="11" cy="11" r="7"/><path d="m20 20-4-4"/>',
  bell: '<path d="M18 8a6 6 0 0 0-12 0c0 7-3 7-3 9h18c0-2-3-2-3-9M10 21h4"/>',
  menu: '<path d="M4 6h16M4 12h16M4 18h16"/>',
  close: '<path d="m18 6-12 12M6 6l12 12"/>',
  plus: '<path d="M12 5v14M5 12h14"/>',
  arrow: '<path d="M5 12h14M13 6l6 6-6 6"/>',
  chevron: '<path d="m9 18 6-6-6-6"/>',
  down: '<path d="m6 9 6 6 6-6"/>',
  book: '<path d="M4 19.5A2.5 2.5 0 0 1 6.5 17H20"/><path d="M6.5 2H20v20H6.5A2.5 2.5 0 0 1 4 19.5v-15A2.5 2.5 0 0 1 6.5 2z"/>',
  users: '<path d="M16 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2"/><circle cx="10" cy="7" r="4"/><path d="M20 21v-2a4 4 0 0 0-3-3.87M16 3.13a4 4 0 0 1 0 7.75"/>',
  trend: '<path d="m3 17 6-6 4 4 8-8"/><path d="M15 7h6v6"/>',
  clock: '<circle cx="12" cy="12" r="9"/><path d="M12 7v5l3 2"/>',
  check: '<path d="m5 12 4 4L19 6"/>',
  sparkles: '<path d="m12 3 1.9 5.8L20 11l-6.1 2.2L12 19l-1.9-5.8L4 11l6.1-2.2L12 3zM19 14l1.2 2.8L23 18l-2.8 1.2L19 22l-1.2-2.8L15 18l2.8-1.2L19 14z"/>',
  cloud: '<path d="M20 16.2A4.5 4.5 0 0 0 18 7.5h-1.3A6 6 0 1 0 5 15.7"/><path d="M12 12v9m-4-4 4-4 4 4"/>',
  document: '<path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"/><path d="M14 2v6h6M8 13h8M8 17h5"/>',
  warning: '<path d="m10.3 3.9-8 14A2 2 0 0 0 4 21h16a2 2 0 0 0 1.7-3.1l-8-14a2 2 0 0 0-3.4 0z"/><path d="M12 9v4m0 4h.01"/>',
  edit: '<path d="M12 20h9"/><path d="M16.5 3.5a2.12 2.12 0 0 1 3 3L8 18l-4 1 1-4Z"/>',
  more: '<circle cx="12" cy="5" r="1"/><circle cx="12" cy="12" r="1"/><circle cx="12" cy="19" r="1"/>',
};

function icon(name: string, className = ''): string {
  return `<svg class="icon ${className}" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">${iconPaths[name] ?? iconPaths.file}</svg>`;
}

function escapeHtml(value: string): string {
  return value.replace(/[&<>"']/g, (character) => {
    const entities: Record<string, string> = {
      '&': '&amp;',
      '<': '&lt;',
      '>': '&gt;',
      '"': '&quot;',
      "'": '&#39;',
    };
    return entities[character] ?? character;
  });
}

function currentPage(): Page {
  const route = window.location.hash.replace(/^#\/?/, '').split('/')[0];
  if (route === 'exams' && window.location.hash.split('/')[2]) {
    return 'exam-detail';
  }
  return ['overview', 'exams', 'upload', 'questions'].includes(route)
    ? (route as Page)
    : 'overview';
}

function currentExam(): Exam | undefined {
  const id = window.location.hash.split('/')[2];
  return id
    ? state.exams.find((exam) => exam.id === id)
    : state.exams[0];
}

function renderLayout(content: string): void {
  const nav = navItems
    .map(
      (item) => `
        <a class="nav-link ${state.page === item.page || (state.page === 'exam-detail' && item.page === 'exams') ? 'active' : ''}" href="${item.href}" ${state.page === item.page ? 'aria-current="page"' : ''}>
          ${icon(item.iconName)}<span>${item.label}</span>
        </a>`,
    )
    .join('');

  root.innerHTML = `
    <div class="app-shell">
      <div class="mobile-scrim ${state.mobileMenuOpen ? 'is-visible' : ''}" data-action="close-menu"></div>
      <aside class="sidebar ${state.mobileMenuOpen ? 'is-open' : ''}" aria-label="Điều hướng chính">
        <a class="brand" href="#/overview" aria-label="TOEIC Reading Admin - Tổng quan">
          <span class="brand-mark">${icon('book')}</span>
          <span class="brand-copy"><strong>TOEIC<span>Reading</span></strong><small>ADMIN CONSOLE</small></span>
        </a>
        <div class="nav-caption">MENU CHÍNH</div>
        <nav class="nav-list">${nav}</nav>
        <div class="sidebar-bottom">
          <div class="sidebar-tip">
            <span class="tip-icon">${icon('sparkles')}</span>
            <strong>Luyện tập hiệu quả</strong>
            <p>Quản lý nội dung học tập trong một nơi.</p>
          </div>
          <div class="sidebar-user">
            <span class="avatar avatar-small">QT</span>
            <span><strong>${api.isAuthenticated() ? 'Quản trị viên' : 'Chưa đăng nhập'}</strong><small>${api.isAuthenticated() ? 'Quản lý hệ thống' : 'Cần xác thực Admin'}</small></span>
            ${icon('more', 'user-more')}
          </div>
        </div>
      </aside>
      <div class="main-shell">
        <header class="topbar">
          <button class="icon-button mobile-menu-button" type="button" data-action="toggle-menu" aria-label="Mở menu">${icon('menu')}</button>
          <div class="topbar-crumb"><span>Quản trị</span>${icon('chevron')}<strong>${pageTitles[state.page]}</strong></div>
          <form class="global-search" id="global-search-form" role="search">
            ${icon('search')}
            <input name="query" type="search" aria-label="Tìm kiếm" placeholder="Tìm kiếm đề thi, câu hỏi..." />
            <kbd>⌘ K</kbd>
          </form>
          <button class="icon-button notification-button" type="button" aria-label="Thông báo">${icon('bell')}<span class="notification-dot"></span></button>
          <button class="user-chip" type="button" data-action="${api.isAuthenticated() ? 'sign-out' : 'show-login'}" aria-label="${api.isAuthenticated() ? 'Đăng xuất' : 'Đăng nhập Admin'}"><span class="avatar">QT</span><span class="user-name">${api.isAuthenticated() ? 'Quản trị viên' : 'Đăng nhập'}</span>${icon('down')}</button>
        </header>
        <main class="page-content">
          ${state.loading ? loadingView() : state.error ? errorView() : content}
        </main>
      </div>
      <div id="toast-region" class="toast-region" aria-live="polite"></div>
      ${state.modalOpen ? renderExamModal() : ''}
    </div>`;
}

function loadingView(): string {
  return '<div class="loading-card"><span class="spinner"></span><span>Đang tải dữ liệu…</span></div>';
}

function errorView(): string {
  const heading = state.accessDenied
    ? 'Tài khoản không có quyền Admin'
    : state.requiresAuth
      ? 'Cần đăng nhập Admin'
      : 'Không thể tải dữ liệu';
  return `<section class="error-card"><span class="error-icon">${icon('warning')}</span><div><h2>${heading}</h2><p>${escapeHtml(state.error)}</p>${state.requiresAuth ? `<form id="admin-login" class="login-form"><label class="field-label">Tài khoản hoặc email<input name="userNameOrEmailAddress" autocomplete="username" required /></label><label class="field-label">Mật khẩu<input name="password" type="password" autocomplete="current-password" required /></label>${state.loginError ? `<p class="upload-error">${escapeHtml(state.loginError)}</p>` : ''}<button class="button button-primary" type="submit">Đăng nhập</button></form>` : ''}<button class="button button-secondary" data-action="retry-load" type="button">Thử lại</button></div></section>`;
}

function sampleBanner(): string {
  return usingSampleData
    ? `<div class="sample-banner">${icon('sparkles')}<span><strong>DỮ LIỆU MẪU</strong> · Nội dung chỉ minh họa trên giao diện, chưa kết nối hoặc lưu vào SQL Server.</span></div>`
    : '';
}

function pageHeading(title: string, description: string, action = ''): string {
  return `<div class="page-heading"><div><div class="eyebrow">TOEIC READING · QUẢN TRỊ</div><h1>${title}</h1><p>${description}</p></div>${action}</div>`;
}

function renderOverview(): string {
  const summary = state.dashboard;
  const isLiveApi = !usingSampleData;
  const cards = [
    { label: 'Tổng số đề thi', value: summary.examCount.toLocaleString('vi-VN'), trend: isLiveApi ? '' : '+12,5%', iconName: 'file', tone: 'indigo', caption: isLiveApi ? 'Từ danh sách đề thi Admin' : 'so với tháng trước' },
    { label: 'Ngân hàng câu hỏi', value: summary.questionCount.toLocaleString('vi-VN'), trend: isLiveApi ? '' : '+8,2%', iconName: 'book', tone: 'violet', caption: isLiveApi ? 'Từ API chi tiết đề thi' : 'so với tháng trước' },
    { label: 'Lượt làm bài', value: isLiveApi ? '—' : summary.attemptCount.toLocaleString('vi-VN'), trend: isLiveApi ? '' : '+18,4%', iconName: 'users', tone: 'blue', caption: isLiveApi ? 'Backend chưa cung cấp API thống kê' : 'so với tháng trước' },
    { label: 'Tỷ lệ hoàn thành', value: isLiveApi ? '—' : `${summary.completionRate}%`, trend: isLiveApi ? '' : '+2,4%', iconName: 'trend', tone: 'green', caption: isLiveApi ? 'Backend chưa cung cấp API thống kê' : 'so với tháng trước' },
  ];
  const activities = state.activities
    .map(
      (activity) => `
        <div class="activity-row">
          <span class="activity-indicator ${activity.tone}">${icon(activity.tone === 'green' ? 'check' : activity.tone === 'violet' ? 'clock' : 'file')}</span>
          <div class="activity-copy"><strong>${escapeHtml(activity.title)}</strong><span>${escapeHtml(activity.detail)}</span></div>
          <time>${escapeHtml(activity.time)}</time>
        </div>`,
    )
    .join('') || (isLiveApi ? '<div class="empty-state">Backend hiện chưa cung cấp API hoạt động gần đây.</div>' : '');

  return `
    ${sampleBanner()}
    ${pageHeading('Tổng quan', 'Theo dõi tình hình học tập và nội dung TOEIC Reading của bạn.')}
    <section class="stats-grid" aria-label="Chỉ số tổng quan">
      ${cards
        .map(
          (card) => `
            <article class="stat-card">
              <div class="stat-top"><span>${card.label}</span><span class="stat-icon ${card.tone}">${icon(card.iconName)}</span></div>
              <div class="stat-value">${card.value}</div>
              <div class="stat-foot">${card.trend ? `<span class="trend-pill">${icon('trend')}${card.trend}</span>` : ''}<span>${card.caption}</span></div>
            </article>`,
        )
        .join('')}
    </section>
    <section class="overview-grid">
      <article class="panel chart-panel">
        <div class="panel-heading"><div><h2>Kết quả làm bài</h2><p>${isLiveApi ? 'Thống kê kết quả từ backend' : 'Xu hướng điểm trung bình trong tuần'}</p></div>${isLiveApi ? '' : `<button class="select-control" type="button">7 ngày qua ${icon('down')}</button>`}</div>
        ${isLiveApi ? '<div class="result-placeholder">Backend hiện chưa cung cấp API thống kê kết quả làm bài.</div>' : `<div class="chart-legend"><span><i></i>Điểm trung bình</span><strong>76,8<span>/100</span></strong></div>${renderChart()}<div class="chart-x-labels"><span>T2</span><span>T3</span><span>T4</span><span>T5</span><span>T6</span><span>T7</span><span>CN</span></div>`}
      </article>
      <article class="panel activity-panel">
        <div class="panel-heading"><div><h2>Hoạt động gần đây</h2><p>Cập nhật mới nhất trong hệ thống</p></div><a href="#/exams" class="text-link">Xem tất cả ${icon('arrow')}</a></div>
        <div class="activity-list">${activities}</div>
      </article>
    </section>
    <section class="panel overview-exams">
      <div class="panel-heading"><div><h2>Đề thi gần đây</h2><p>Một số đề thi được cập nhật gần đây</p></div><a href="#/exams" class="text-link">Quản lý đề thi ${icon('arrow')}</a></div>
      ${examTable(state.exams.slice(0, 3), false)}
    </section>`;
}

function renderChart(): string {
  const values = [46, 58, 53, 72, 65, 82, 77];
  const points = values
    .map((value, index) => `${54 + index * 108},${166 - value * 1.45}`)
    .join(' ');
  const areaPath = `M ${points.replaceAll(' ', ' L ')} L 702 175 L 54 175 Z`;
  return `
    <div class="chart-area">
      <div class="chart-y-labels"><span>100</span><span>75</span><span>50</span><span>25</span><span>0</span></div>
      <svg class="result-chart" viewBox="0 0 756 190" role="img" aria-label="Biểu đồ điểm trung bình trong tuần">
        <defs><linearGradient id="chart-fill" x1="0" x2="0" y1="0" y2="1"><stop offset="0%" stop-color="#5146e5" stop-opacity=".19"/><stop offset="100%" stop-color="#5146e5" stop-opacity="0"/></linearGradient></defs>
        <path d="M54 30H702M54 66H702M54 102H702M54 138H702M54 174H702" class="chart-gridline"/>
        <path d="${areaPath}" fill="url(#chart-fill)"/>
        <polyline points="${points}" class="chart-line"/>
        ${values.map((value, index) => `<circle cx="${54 + index * 108}" cy="${166 - value * 1.45}" r="4" class="chart-point"/>`).join('')}
      </svg>
    </div>`;
}

function renderExams(): string {
  const filtered = state.exams.filter((exam) => {
    const matchesText = `${exam.title} ${exam.id}`.toLocaleLowerCase('vi').includes(state.examSearch.toLocaleLowerCase('vi'));
    return matchesText && (state.examStatus === 'all' || exam.status === state.examStatus);
  });
  const pageSize = 5;
  const pages = Math.max(1, Math.ceil(filtered.length / pageSize));
  const page = Math.min(state.examPage, pages);
  const rows = filtered.slice((page - 1) * pageSize, page * pageSize);

  return `
    ${sampleBanner()}
    ${pageHeading('Quản lý đề thi', 'Tạo, chỉnh sửa và theo dõi trạng thái xuất bản của các đề thi.', '<button class="button button-primary" type="button" data-action="open-exam-modal">' + icon('plus') + ' Tạo đề thi mới</button>')}
    <section class="panel table-panel">
      <div class="table-toolbar">
        <label class="search-field">${icon('search')}<input data-search="exams" type="search" value="${escapeHtml(state.examSearch)}" placeholder="Tìm theo tên hoặc mã đề..." aria-label="Tìm đề thi" /></label>
        <div class="filter-row">
          <label class="select-wrap"><span class="sr-only">Lọc trạng thái</span><select data-filter="exam-status"><option value="all" ${state.examStatus === 'all' ? 'selected' : ''}>Tất cả trạng thái</option><option value="Đã xuất bản" ${state.examStatus === 'Đã xuất bản' ? 'selected' : ''}>Đã xuất bản</option><option value="Bản nháp" ${state.examStatus === 'Bản nháp' ? 'selected' : ''}>Bản nháp</option></select>${icon('down')}</label>
          <button class="button button-secondary filter-button" type="button" data-action="reset-exam-filters">${icon('close')} Xóa lọc</button>
        </div>
      </div>
      ${examTable(rows, true)}
      <div class="table-footer"><span>Hiển thị <strong>${filtered.length ? (page - 1) * pageSize + 1 : 0}–${Math.min(page * pageSize, filtered.length)}</strong> trong <strong>${filtered.length}</strong> đề thi${usingSampleData ? '<span class="sample-inline"> · dữ liệu mẫu</span>' : ''}</span><div class="pagination"><button type="button" aria-label="Trang trước" data-action="exam-page" data-page="${Math.max(1, page - 1)}" ${page <= 1 ? 'disabled' : ''}>${icon('chevron', 'rotate-180')}</button><span>Trang <strong>${page}</strong> / ${pages}</span><button type="button" aria-label="Trang sau" data-action="exam-page" data-page="${Math.min(pages, page + 1)}" ${page >= pages ? 'disabled' : ''}>${icon('chevron')}</button></div></div>
    </section>`;
}

function examTable(exams: Exam[], showActions: boolean): string {
  if (!exams.length) {
    return `<div class="empty-state">${icon('search')}<strong>Không tìm thấy đề thi</strong><span>Thử thay đổi từ khóa hoặc bộ lọc.</span></div>`;
  }
  return `
    <div class="table-scroll">
      <table class="data-table">
        <thead><tr><th>Mã đề thi</th><th>Tiêu đề</th><th>Thời lượng</th><th>Số câu hỏi</th><th>Ngày tạo</th><th>Trạng thái</th>${showActions ? '<th class="action-column">Thao tác</th>' : ''}</tr></thead>
        <tbody>
          ${exams
            .map(
              (exam) => `
                <tr>
                  <td><span class="code-label">${escapeHtml(exam.id)}</span></td>
                  <td><a class="table-title" href="#/exams/${encodeURIComponent(exam.id)}">${escapeHtml(exam.title)}</a><small class="table-description">${escapeHtml(exam.description)}</small></td>
                  <td><span class="cell-inline">${icon('clock')}${exam.duration} phút</span></td>
                  <td>${exam.questionCount}</td>
                  <td>${escapeHtml(exam.updatedAt)}</td>
                  <td><span class="status-badge ${exam.status === 'Đã xuất bản' ? 'published' : 'draft'}"><i></i>${exam.status}</span></td>
                  ${showActions ? `<td class="action-column"><button class="button button-secondary row-edit-button" type="button" data-action="edit-exam" data-id="${escapeHtml(exam.id)}">${icon('edit')} Chỉnh sửa</button><button class="icon-button row-action" type="button" data-action="exam-more" data-id="${escapeHtml(exam.id)}" aria-label="Chi tiết đề thi">${icon('more')}</button></td>` : ''}
                </tr>`,
            )
            .join('')}
        </tbody>
      </table>
    </div>`;
}

function renderUpload(): string {
  const selected = state.uploadFile;
  const statusLabels: Record<UploadStatus, string> = {
    empty: 'Chưa chọn tệp',
    ready: 'Sẵn sàng đọc',
    processing: 'Đang đọc file',
    complete: 'Đã phân tích',
    importing: 'Đang nhập',
    imported: 'Đã nhập',
    error: 'Có lỗi',
  };
  const preview = state.uploadPreview;
  const issues = getUploadIssues(preview);
  const canImport =
    Boolean(preview) &&
    issues.length === 0 &&
    state.uploadStatus === 'complete' &&
    !usingSampleData;
  return `
    ${pageHeading('Tải đề từ Word', 'Nhập nội dung đề thi từ tệp DOCX và kiểm tra trước khi đưa vào thư viện.')}
    <div class="upload-layout">
      <section class="panel upload-panel">
        <div class="panel-heading"><div><h2>Tải lên tài liệu</h2><p>Chọn tệp Word có cấu trúc đề TOEIC Reading.</p></div><span class="step-pill">BƯỚC 1 / 2</span></div>
        <label class="dropzone" for="docx-file">
          <span class="dropzone-icon">${icon('cloud')}</span>
          <strong>Kéo thả tệp DOCX vào đây</strong>
          <span>hoặc <span class="text-link">chọn tệp từ máy tính</span></span>
          <small>Hỗ trợ .docx · tối đa 10 MB</small>
          <input id="docx-file" data-file-input type="file" accept=".docx,application/vnd.openxmlformats-officedocument.wordprocessingml.document" />
        </label>
        ${
          selected
            ? `<div class="file-card"><span class="file-icon">${icon('document')}</span><div class="file-info"><strong>${escapeHtml(selected.name)}</strong><span>${formatBytes(selected.size)}</span><div class="progress-track"><span style="width:${state.uploadStatus === 'complete' || state.uploadStatus === 'imported' ? '100%' : state.uploadStatus === 'processing' || state.uploadStatus === 'importing' ? '68%' : '0%'}"></span></div></div><span class="file-status ${state.uploadStatus === 'complete' || state.uploadStatus === 'imported' ? 'done' : ''}">${state.uploadStatus === 'processing' || state.uploadStatus === 'importing' ? '<span class="spinner spinner-small"></span>' : icon(state.uploadStatus === 'complete' || state.uploadStatus === 'imported' ? 'check' : 'clock')}${statusLabels[state.uploadStatus]}</span><button class="icon-button file-remove" data-action="remove-file" type="button" aria-label="Bỏ tệp" ${state.uploadStatus === 'processing' || state.uploadStatus === 'importing' ? 'disabled' : ''}>${icon('close')}</button></div>`
            : `<div class="upload-hint">${icon('warning')}<span>Chọn DOCX có các thẻ [TITLE], [DURATION], [QUESTION] và [END EXAM]. File sẽ được tải lên backend để phân tích.</span></div>`
        }
        <div class="upload-actions"><button class="button button-primary" type="button" data-action="process-upload" ${!selected || state.uploadStatus === 'processing' || state.uploadStatus === 'importing' || state.uploadStatus === 'imported' ? 'disabled' : ''}>${icon('sparkles')} Đọc file Word</button><span class="helper-text">${usingSampleData ? 'Đang bật dữ liệu mẫu: cần chuyển sang API thật để phân tích và lưu.' : 'Cần đăng nhập bằng tài khoản được cấp quyền quản trị để phân tích và nhập đề.'}</span></div>
        ${state.uploadError ? `<div class="notice-box upload-error">${icon('warning')}<div><strong>Không thể xử lý file</strong><p>${escapeHtml(state.uploadError)}</p></div></div>` : ''}
      </section>
      <section class="panel import-panel">
        <div class="panel-heading"><div><h2>Kết quả nhập</h2><p>Tóm tắt phân tích nội dung tài liệu</p></div><span class="step-pill">BƯỚC 2 / 2</span></div>
        ${
          preview
            ? `<div class="import-success">${icon(issues.length ? 'warning' : 'check')}<div><strong>${issues.length ? 'Phân tích xong, cần xử lý lỗi' : 'Đã phân tích nội dung file'}</strong><span>${escapeHtml(preview.title || 'Chưa có tiêu đề')} · ${preview.durationMinutes || '—'} phút</span></div></div><div class="result-counts"><div><strong>${preview.readingPassages.length}</strong><span>Bài đọc</span></div><div><strong>${preview.questions.length}</strong><span>Câu hỏi</span></div><div><strong>${issues.length}</strong><span>Lỗi cần sửa</span></div></div>${preview.warnings.length ? `<div class="notice-box">${icon('warning')}<div><strong>Cảnh báo</strong><ul>${preview.warnings.map((warning) => `<li>${escapeHtml(warning.message)}</li>`).join('')}</ul></div></div>` : ''}${state.uploadResult ? `<div class="notice-box">${icon('check')}<div><strong>Đã lưu thành công</strong><p>${escapeHtml(state.uploadResult.message)} Mã đề: ${state.uploadResult.examId} · ${state.uploadResult.passageCount} bài đọc · ${state.uploadResult.questionCount} câu hỏi.</p></div></div>` : issues.length ? `<div class="notice-box upload-error">${icon('warning')}<div><strong>Cần rà soát trước khi nhập</strong><ul>${issues.map((issue) => `<li>${escapeHtml(issue.message)}</li>`).join('')}</ul></div></div>` : `<div class="notice-box">${icon('check')}<div><strong>Dữ liệu hợp lệ</strong><p>Đề sẽ được lưu ở trạng thái bản nháp sau khi xác nhận.</p></div></div>`}`
            : `<div class="result-placeholder">${icon('document')}<strong>Chưa có kết quả</strong><span>Chọn file DOCX và bấm “Đọc file Word” để phân tích nội dung thật.</span></div>`
        }
        <div class="import-actions"><button class="button button-secondary" type="button" data-action="preview-import" ${!preview ? 'disabled' : ''}>${state.uploadPreviewOpen ? 'Ẩn xem trước' : 'Xem trước nội dung'}</button><button class="button button-primary" type="button" data-action="save-import" ${!canImport ? 'disabled' : ''}>Nhập đề</button></div>
        ${state.uploadPreviewOpen && preview ? renderWordPreview(preview) : ''}
      </section>
    </div>`;
}

function getUploadIssues(preview: WordExamPreview | null) {
  if (!preview) return [];
  const issues = [
    ...preview.errors,
    ...preview.questions.flatMap((question) =>
      question.errors.filter(
        (issue) =>
          issue.code !== 'question.option.missing' &&
          issue.code !== 'question.answer.required' &&
          issue.code !== 'question.answer.option_missing'
      )
    ),
  ];
  for (const question of preview.questions) {
    const options = getWordOptionMap(question);
    for (const label of ['A', 'B', 'C', 'D']) {
      if (!options.get(label)?.trim()) {
        issues.push({
          code: 'question.option.missing',
          message: `Câu ${question.questionNumber}: thiếu nội dung phương án ${label}.`,
          questionNumber: question.questionNumber,
        });
      }
    }
    const answer = question.answer.trim().toUpperCase();
    if (!['A', 'B', 'C', 'D'].includes(answer)) {
      issues.push({
        code: 'question.answer.required',
        message: `Câu ${question.questionNumber}: hãy xác nhận đáp án đúng A, B, C hoặc D.`,
        questionNumber: question.questionNumber,
      });
    } else if (!options.get(answer)?.trim()) {
      issues.push({
        code: 'question.answer.option_missing',
        message: `Câu ${question.questionNumber}: đáp án ${answer} chưa có nội dung phương án.`,
        questionNumber: question.questionNumber,
      });
    }
  }
  return issues;
}

function getWordOptionMap(
  question: WordExamPreview['questions'][number],
): Map<string, string> {
  return new Map(
    (question.options ?? [])
      .filter((option) => option != null)
      .map((option) => [
        String(option.label ?? '').trim().toUpperCase(),
        String(option.content ?? '').replace(/[\r\n]+/g, ' ').replace(/\s{2,}/g, ' ').trim(),
      ])
  );
}

function renderWordPreview(preview: WordExamPreview): string {
  return `
    <div class="word-preview-list">
      <div class="field-group"><label for="word-preview-title">Tiêu đề</label><input id="word-preview-title" value="${escapeHtml(preview.title)}" readonly /></div>
      <div class="field-group"><label for="word-preview-description">Mô tả</label><textarea id="word-preview-description" rows="2" readonly>${escapeHtml(preview.description)}</textarea></div>
      ${preview.readingPassages.map((passage) => `<article class="word-preview-passage"><h3>Part ${passage.partNumber}: ${escapeHtml(passage.title)}</h3><p>${escapeHtml(passage.content)}</p></article>`).join('')}
      ${preview.questions.map((question, index) => {
        const options = getWordOptionMap(question);
        const passage = preview.readingPassages.find((item) => item.tempId === question.readingPassageTempId);
        return `<article class="word-preview-question"><h3>Câu ${question.questionNumber} · Part ${question.partNumber}${passage ? ` · ${escapeHtml(passage.title)}` : ''}</h3><p>${escapeHtml(question.questionText)}</p><div class="word-preview-options">${(['A', 'B', 'C', 'D'] as const).map((label) => `<label><span>${label}</span><input data-import-option="${index}-${label}" value="${escapeHtml(options.get(label) ?? '')}" aria-label="Phương án ${label} của câu ${question.questionNumber}" /></label>`).join('')}</div><label class="field-group word-answer-field"><span>Đáp án đúng đã xác nhận</span><select data-import-answer="${index}"><option value="">Chưa xác định</option>${(['A', 'B', 'C', 'D'] as const).map((label) => `<option value="${label}" ${question.answer.toUpperCase() === label ? 'selected' : ''}>${label}</option>`).join('')}</select></label>${question.explanation ? `<p class="word-explanation"><strong>Giải thích:</strong> ${escapeHtml(question.explanation)}</p>` : ''}${question.hint ? `<p class="word-explanation"><strong>Gợi ý:</strong> ${escapeHtml(question.hint)}</p>` : ''}</article>`;
      }).join('')}
    </div>`;
}

function renderQuestions(): string {
  const filtered = state.questions.filter((question) => {
    const matchesText = `${question.id} ${question.prompt} ${question.passageTitle}`
      .toLocaleLowerCase('vi')
      .includes(state.questionSearch.toLocaleLowerCase('vi'));
    return matchesText && (state.questionPart === 'all' || question.part === state.questionPart);
  });
  const pageSize = 4;
  const pages = Math.max(1, Math.ceil(filtered.length / pageSize));
  const page = Math.min(state.questionPage, pages);
  const visibleQuestions = filtered.slice((page - 1) * pageSize, page * pageSize);
  const selected = state.questions.find((question) => question.id === state.selectedQuestionId) ?? visibleQuestions[0] ?? state.questions[0];

  return `
    ${sampleBanner()}
    ${pageHeading('Quản lý câu hỏi', 'Quản lý bài đọc, nội dung câu hỏi và đáp án TOEIC Reading.', `<button class="button button-primary" type="button" data-action="new-question">${icon('plus')} Thêm câu hỏi</button>`)}
    <section class="question-workspace">
      <div class="question-list-panel panel">
        <div class="question-toolbar">
          <label class="search-field">${icon('search')}<input data-search="questions" type="search" value="${escapeHtml(state.questionSearch)}" placeholder="Tìm nội dung câu hỏi..." aria-label="Tìm câu hỏi" /></label>
          <label class="select-wrap"><span class="sr-only">Lọc phần thi</span><select data-filter="question-part"><option value="all" ${state.questionPart === 'all' ? 'selected' : ''}>Tất cả Part</option>${['Part 5', 'Part 6', 'Part 7'].map((part) => `<option value="${part}" ${state.questionPart === part ? 'selected' : ''}>${part}</option>`).join('')}</select>${icon('down')}</label>
        </div>
        <div class="question-list">
          ${
            visibleQuestions.length
              ? visibleQuestions
                  .map(
                    (question) => `
                      <button class="question-list-item ${question.id === selected?.id ? 'selected' : ''}" type="button" data-action="select-question" data-id="${escapeHtml(question.id)}">
                        <span class="question-item-top"><span class="code-label">${escapeHtml(question.id)}</span><span class="part-badge">${escapeHtml(question.part)}</span></span>
                        <strong>${escapeHtml(question.prompt)}</strong>
                        <span class="question-passage-name">${escapeHtml(question.passageTitle)}</span>
                      </button>`,
                  )
                  .join('')
              : `<div class="empty-state compact">${icon('search')}<strong>Không có câu hỏi phù hợp</strong><span>Thử đổi từ khóa hoặc bộ lọc.</span></div>`
          }
        </div>
        <div class="question-list-footer"><span>${filtered.length} câu hỏi${usingSampleData ? ' mẫu' : ''}</span><div class="pagination"><button type="button" aria-label="Trang trước" data-action="question-page" data-page="${Math.max(1, page - 1)}" ${page <= 1 ? 'disabled' : ''}>${icon('chevron', 'rotate-180')}</button><span>${page}/${pages}</span><button type="button" aria-label="Trang sau" data-action="question-page" data-page="${Math.min(pages, page + 1)}" ${page >= pages ? 'disabled' : ''}>${icon('chevron')}</button></div></div>
      </div>
      ${selected ? renderQuestionEditor(selected) : '<div class="panel editor-empty">Chọn một câu hỏi để xem hoặc chỉnh sửa.</div>'}
    </section>`;
}

function renderQuestionEditor(question: Question): string {
  return `
    <form class="panel question-editor" id="question-form">
      <div class="panel-heading editor-heading"><div><h2>Chi tiết câu hỏi</h2><p><span class="code-label">${escapeHtml(question.id)}</span><span class="part-badge">${escapeHtml(question.part)}</span></p></div><button class="icon-button" type="button" aria-label="Tùy chọn câu hỏi">${icon('more')}</button></div>
      <label class="field-label">Phần thi <span class="required">*</span><select name="part" required>${['Part 5', 'Part 6', 'Part 7'].map((part) => `<option value="${part}" ${question.part === part ? 'selected' : ''}>${part}</option>`).join('')}</select></label>
      <label class="field-label">Bài đọc <span class="required">*</span><input name="passageTitle" value="${escapeHtml(question.passageTitle)}" required maxlength="120" /></label>
      <label class="field-label">Nội dung bài đọc <span class="required">*</span><textarea name="passage" rows="3" required>${escapeHtml(question.passage)}</textarea></label>
      <label class="field-label">Nội dung câu hỏi <span class="required">*</span><textarea name="prompt" rows="2" required>${escapeHtml(question.prompt)}</textarea></label>
      <fieldset class="answer-fieldset"><legend>Các lựa chọn <span class="required">*</span><small>Chọn đáp án đúng</small></legend>
        ${(['A', 'B', 'C', 'D'] as const).map((letter) => `<label class="option-row ${question.correctAnswer === letter ? 'correct' : ''}"><input type="radio" name="correctAnswer" value="${letter}" ${question.correctAnswer === letter ? 'checked' : ''} required /><span class="option-letter">${letter}</span><input name="option${letter}" aria-label="Lựa chọn ${letter}" value="${escapeHtml(question.options[letter])}" required maxlength="240" /><span class="correct-label">${question.correctAnswer === letter ? 'Đáp án đúng' : ''}</span></label>`).join('')}
      </fieldset>
      <div class="form-two-columns"><label class="field-label">Gợi ý<textarea name="hint" rows="2" placeholder="Gợi ý để học viên suy luận">${escapeHtml(question.hint)}</textarea></label><label class="field-label">Giải thích đáp án <span class="required">*</span><textarea name="explanation" rows="2" required>${escapeHtml(question.explanation)}</textarea></label></div>
      <div class="form-note">${icon('warning')}${usingSampleData ? 'Thay đổi trong bản demo chỉ ở bộ nhớ trình duyệt, chưa lưu vào SQL Server.' : question.correctAnswer ? question.id === state.newQuestionId ? 'API backend hỗ trợ tạo câu hỏi mới.' : 'Backend hiện chưa cung cấp API cập nhật câu hỏi; thao tác lưu sẽ bị từ chối.' : 'Backend không trả đáp án đúng cho câu hỏi này; không có đáp án nào được chọn.'}</div>
      <div class="editor-actions"><button class="button button-secondary" type="reset">Hoàn tác</button><button class="button button-primary" type="submit" ${!usingSampleData && question.id !== state.newQuestionId ? 'disabled' : ''}>${icon('check')} ${usingSampleData ? 'Lưu câu hỏi mẫu' : question.id === state.newQuestionId ? 'Tạo câu hỏi' : 'Cập nhật chưa được hỗ trợ'}</button></div>
    </form>`;
}

function getPublicationBlockReason(
  detail: AdminExamDetail | undefined,
): string {
  if (usingSampleData) {
    return 'Đang dùng dữ liệu mẫu; hãy kết nối backend thật để lưu trạng thái vào SQL Server.';
  }
  if (!detail) {
    return 'Chưa tải được chi tiết đề thi để kiểm tra điều kiện kích hoạt.';
  }
  if (detail.questions.length === 0) {
    return 'Cần có ít nhất một câu hỏi trước khi kích hoạt.';
  }

  for (const question of detail.questions) {
    if (![5, 6, 7].includes(question.partNumber)) {
      return `Câu ${question.questionNumber}: PartNumber không hợp lệ.`;
    }

    const passage =
      question.partNumber === 6 || question.partNumber === 7
        ? detail.readingPassages.find(
            (item) =>
              item.id === question.readingPassageId &&
              item.partNumber === question.partNumber,
          )
        : undefined;

    if (
      (question.partNumber === 5 || question.partNumber === 7) &&
      !question.questionText.trim()
    ) {
      return `Câu ${question.questionNumber}: thiếu nội dung câu hỏi (QuestionText).`;
    }
    if (
      (question.partNumber === 6 || question.partNumber === 7) &&
      (!question.readingPassageId || !passage || !passage.content.trim())
    ) {
      return `Câu ${question.questionNumber}: thiếu bài đọc hợp lệ (ReadingPassageId/Content).`;
    }
    if (
      question.partNumber === 6 &&
      !question.questionText.trim() &&
      !new RegExp(String.raw`\[\s*${question.questionNumber}\s*\]`).test(
        passage?.content ?? '',
      )
    ) {
      return `Câu ${question.questionNumber}: thiếu nội dung câu hỏi (QuestionText) hoặc chỗ trống [${question.questionNumber}] trong bài đọc (Content).`;
    }

    const options = question.options ?? [];
    const labels = options.map((option) => option.label.trim().toUpperCase());
    if (
      options.length !== 4 ||
      new Set(labels).size !== 4 ||
      !(['A', 'B', 'C', 'D'] as const).every((label) => labels.includes(label))
    ) {
      return `Câu ${question.questionNumber} cần đủ phương án A, B, C, D và không trùng nhãn.`;
    }
    if (options.some((option) => !option.content.trim())) {
      const missingOption = options.find((option) => !option.content.trim());
      return `Câu ${question.questionNumber}: phương án ${missingOption?.label || 'không xác định'} thiếu nội dung (Content).`;
    }
    if (options.filter((option) => option.isCorrect).length !== 1) {
      return `Câu ${question.questionNumber}: cần chính xác một đáp án đúng (IsCorrect).`;
    }
  }

  return '';
}

function renderExamDetail(): string {
  const exam = currentExam();
  if (!exam) {
    return `${pageHeading('Chi tiết đề thi', 'Không tìm thấy đề thi trong danh sách dữ liệu đã tải.')}<div class="panel empty-state">Không có dữ liệu đề thi tương ứng.</div>`;
  }
  if (state.examEditDraft?.id === exam.id) {
    return renderExamEditor(state.examEditDraft);
  }
  const published = exam.status === 'Đã xuất bản';
  const detail = state.examDetails[exam.id];
  const publicationBlockReason = getPublicationBlockReason(detail);
  const publicationUpdating = state.publicationUpdatingExamId === exam.id;
  const partCounts = [
    { number: 5, name: 'Part 5 · Hoàn thành câu' },
    { number: 6, name: 'Part 6 · Hoàn thành đoạn văn' },
    { number: 7, name: 'Part 7 · Đọc hiểu' },
  ].map((part) => {
    const count = detail?.questions.filter((question) => question.partNumber === part.number).length ?? 0;
    return {
      ...part,
      count,
      percent: exam.questionCount > 0 ? Math.round((count / exam.questionCount) * 100) : 0,
    };
  });
  return `
    ${sampleBanner()}
    <div class="detail-breadcrumb"><a href="#/exams">Quản lý đề thi</a>${icon('chevron')}<span>Chi tiết đề thi</span></div>
    <section class="detail-heading">
      <div class="detail-title-wrap"><a class="back-link" href="#/exams">${icon('chevron')} Quay lại danh sách</a><div class="detail-title-line"><h1>${escapeHtml(exam.title)}</h1><span class="status-badge ${published ? 'published' : 'draft'}"><i></i>${exam.status}</span></div><p>Mã đề <span class="code-label">${escapeHtml(exam.id)}</span> <span class="detail-dot">·</span> Tạo ngày ${escapeHtml(exam.updatedAt)}</p></div>
      <div class="detail-actions"><button class="button button-secondary" type="button" data-action="edit-exam">${icon('edit')} Chỉnh sửa</button></div>
    </section>
    <section class="detail-stats">
      <article class="panel detail-stat"><span class="detail-stat-icon indigo">${icon('clock')}</span><div><span>Thời lượng</span><strong>${exam.duration} phút</strong><small>Thời gian làm bài</small></div></article>
      <article class="panel detail-stat"><span class="detail-stat-icon violet">${icon('list')}</span><div><span>Số câu hỏi</span><strong>${exam.questionCount} câu</strong><small>Trong toàn bộ đề thi</small></div></article>
      <article class="panel detail-stat"><span class="detail-stat-icon green">${icon('check')}</span><div><span>Trạng thái</span><strong>${exam.status}</strong><small>Trạng thái hiện tại</small></div></article>
      <article class="panel detail-stat"><span class="detail-stat-icon blue">${icon('file')}</span><div><span>Ngày tạo</span><strong>${escapeHtml(exam.updatedAt)}</strong><small>Ngày tạo từ SQL Server</small></div></article>
    </section>
    <section class="detail-grid">
      <article class="panel detail-description"><div class="panel-heading"><div><h2>Thông tin đề thi</h2><p>Thông tin cơ bản và mô tả</p></div></div><div class="detail-description-body"><span class="detail-label">Mô tả</span><p>${escapeHtml(exam.description)}</p><div class="detail-meta"><div><span class="detail-label">Mã đề</span><strong>${escapeHtml(exam.id)}</strong></div><div><span class="detail-label">Loại nội dung</span><strong>TOEIC Reading</strong></div></div></div></article>
      <article class="panel composition-panel"><div class="panel-heading"><div><h2>Cấu trúc đề thi</h2><p>Phân bố nội dung theo từng phần</p></div><span class="part-badge">3 phần thi</span></div><div class="composition-list">${partCounts.map((part) => `<div class="composition-item"><div class="composition-top"><strong>${part.name}</strong><span>${part.count} câu</span></div><div class="progress-track"><span style="width:${part.percent}%"></span></div></div>`).join('')}</div><div class="composition-footer"><span>Tổng số câu hỏi</span><strong>${exam.questionCount} câu</strong></div></article>
    </section>
    <section class="panel publication-panel"><div class="publication-icon ${published ? 'published' : 'draft'}">${icon(published ? 'check' : 'clock')}</div><div class="publication-copy"><strong>${published ? 'Đề thi đang được xuất bản' : 'Đề thi đang ở trạng thái bản nháp'}</strong><span>${published ? 'Học viên có thể xem và làm đề thi này.' : 'Đề thi chưa hiển thị với học viên.'}</span></div><div class="publication-control"><label class="publication-toggle"><span>Kích hoạt ca thi</span><input type="checkbox" role="switch" data-action="toggle-publish" data-id="${escapeHtml(exam.id)}" aria-label="Kích hoạt ca thi" aria-describedby="publication-toggle-help" ${published ? 'checked' : ''} ${publicationBlockReason || publicationUpdating ? 'disabled' : ''}><span class="publication-toggle-track" aria-hidden="true"></span></label><small id="publication-toggle-help" class="publication-toggle-help">${publicationUpdating ? 'Đang lưu trạng thái...' : escapeHtml(publicationBlockReason || (published ? 'Đã xuất bản' : 'Bản nháp'))}</small></div></section>`;
}

function renderExamEditor(detail: AdminExamDetail): string {
  const part = state.examEditPart;
  const partPassages = detail.readingPassages
    .map((passage, index) => ({ passage, index }))
    .filter(({ passage }) => passage.partNumber === part);
  const partQuestions = detail.questions
    .map((question, index) => ({ question, index }))
    .filter(({ question }) => question.partNumber === part)
    .sort((left, right) => left.question.questionNumber - right.question.questionNumber);

  return `
    ${sampleBanner()}
    <div class="detail-breadcrumb"><a href="#/exams">Quản lý đề thi</a>${icon('chevron')}<a href="#/exams/${encodeURIComponent(detail.id)}">Chi tiết đề thi</a>${icon('chevron')}<span>Chỉnh sửa</span></div>
    <section class="detail-heading">
      <div class="detail-title-wrap"><a class="back-link" href="#/exams/${encodeURIComponent(detail.id)}" data-action="cancel-exam-edit">${icon('chevron')} Hủy chỉnh sửa</a><div class="detail-title-line"><h1>Chỉnh sửa đề thi</h1><span class="status-badge ${detail.status === 'Đã xuất bản' ? 'published' : 'draft'}"><i></i>${escapeHtml(detail.status)}</span></div><p>Mọi thay đổi nội dung chỉ được lưu khi backend xác nhận.</p></div>
      <div class="detail-actions"><button class="button button-secondary" type="button" data-action="cancel-exam-edit">Hủy</button><button class="button button-primary" type="submit" form="exam-editor-form" ${state.examEditSaving ? 'disabled' : ''}>${icon('check')} ${state.examEditSaving ? 'Đang lưu...' : 'Lưu thay đổi'}</button></div>
    </section>
    <form id="exam-editor-form" class="exam-editor">
      <section class="panel editor-section">
        <div class="panel-heading"><div><h2>Thông tin đề thi</h2><p>Mã đề ${escapeHtml(detail.id)} · ${detail.questions.length} câu hỏi</p></div></div>
        <div class="exam-editor-fields">
          <label class="field-label">Tên đề thi <span class="required">*</span><input required maxlength="200" data-edit-field="title" value="${escapeHtml(detail.title)}" /></label>
          <label class="field-label">Thời lượng (phút) <span class="required">*</span><input required type="number" min="1" max="300" data-edit-field="duration" value="${detail.duration}" /></label>
          <label class="field-label editor-description">Mô tả<textarea rows="3" maxlength="4000" data-edit-field="description">${escapeHtml(detail.description)}</textarea></label>
        </div>
      </section>
      <section class="panel editor-section">
        <div class="panel-heading editor-toolbar"><div><h2>Bài đọc và câu hỏi</h2><p>Sửa nội dung trong đề hiện có; không thay đổi trạng thái xuất bản.</p></div><label class="field-label editor-part-filter">Xem phần<select data-edit-part><option value="5" ${part === 5 ? 'selected' : ''}>Part 5</option><option value="6" ${part === 6 ? 'selected' : ''}>Part 6</option><option value="7" ${part === 7 ? 'selected' : ''}>Part 7</option></select></label></div>
        ${part === 5 ? '<p class="editor-hint">Part 5 không sử dụng bài đọc.</p>' : `
        <div class="editor-subheading"><div><h3>Bài đọc Part ${part}</h3><p>Các câu hỏi liên kết sẽ giữ đúng bài đọc đã chọn.</p></div><button class="button button-secondary" type="button" data-action="add-exam-passage">${icon('plus')} Thêm bài đọc</button></div>
        <div class="editor-passage-list">${partPassages.length ? partPassages.map(({ passage, index }) => {
          const hasQuestions = detail.questions.some((question) => question.readingPassageId === passage.id);
          return `<article class="editor-card">
            <div class="editor-card-heading"><strong>Bài đọc ${passage.orderNumber}</strong><button class="button button-danger" type="button" data-action="remove-exam-passage" data-index="${index}" ${hasQuestions ? 'disabled title="Bài đọc đang được câu hỏi sử dụng."' : ''}>Xóa bài đọc</button></div>
            <div class="editor-inline-fields"><label class="field-label">Tiêu đề<input required maxlength="200" data-edit-field="passage-title" data-index="${index}" value="${escapeHtml(passage.title)}" /></label><label class="field-label">Thứ tự<input required type="number" min="1" data-edit-field="passage-order" data-index="${index}" value="${passage.orderNumber}" /></label></div>
            <label class="field-label">Nội dung bài đọc <span class="required">*</span><textarea required rows="7" data-edit-field="passage-content" data-index="${index}">${escapeHtml(passage.content)}</textarea></label>
          </article>`;
        }).join('') : '<div class="empty-state">Chưa có bài đọc cho Part này. Hãy thêm bài đọc trước khi thêm câu hỏi.</div>'}</div>`}
        <div class="editor-subheading editor-question-heading"><div><h3>Câu hỏi Part ${part}</h3><p>${partQuestions.length} câu trong phần này</p></div><button class="button button-secondary" type="button" data-action="add-exam-question" ${part !== 5 && partPassages.length === 0 ? 'disabled title="Thêm bài đọc trước."' : ''}>${icon('plus')} Thêm câu hỏi</button></div>
        <div class="editor-question-list">${partQuestions.length ? partQuestions.map(({ question, index }) => `
          <article class="editor-card editor-question-card">
            <div class="editor-card-heading"><strong>Câu hỏi ${question.questionNumber}</strong><button class="button button-danger" type="button" data-action="remove-exam-question" data-index="${index}">Xóa câu hỏi</button></div>
            <div class="editor-inline-fields"><label class="field-label">Part <span class="required">*</span><select required data-edit-field="question-part" data-index="${index}"><option value="5" ${question.partNumber === 5 ? 'selected' : ''}>Part 5</option><option value="6" ${question.partNumber === 6 ? 'selected' : ''}>Part 6</option><option value="7" ${question.partNumber === 7 ? 'selected' : ''}>Part 7</option></select></label><label class="field-label">Số câu <span class="required">*</span><input required type="number" min="1" data-edit-field="question-number" data-index="${index}" value="${question.questionNumber}" /></label>
              ${part === 5 ? '' : `<label class="field-label">Bài đọc <span class="required">*</span><select required data-edit-field="question-passage" data-index="${index}"><option value="">Chọn bài đọc</option>${partPassages.map(({ passage }) => `<option value="${passage.id}" ${question.readingPassageId === passage.id ? 'selected' : ''}>${escapeHtml(passage.title || `Bài đọc ${passage.orderNumber}`)}</option>`).join('')}</select></label>`}
            </div>
            <label class="field-label">Nội dung câu hỏi${part === 6 ? '' : ' *'}<textarea ${part !== 6 ? 'required' : ''} rows="3" data-edit-field="question-text" data-index="${index}" placeholder="${part === 6 ? 'Có thể để trống nếu chỗ trống [số câu] nằm trong bài đọc.' : ''}">${escapeHtml(question.questionText)}</textarea></label>
            <div class="editor-options"><strong>Phương án A/B/C/D <span class="required">*</span></strong>${question.options.slice().sort((a, b) => a.label.localeCompare(b.label)).map((option) => {
              const optionIndex = question.options.indexOf(option);
              return `<label class="editor-option"><select aria-label="Nhãn phương án" data-edit-field="option-label" data-index="${index}" data-option-index="${optionIndex}"><option value="A" ${option.label.toUpperCase() === 'A' ? 'selected' : ''}>A</option><option value="B" ${option.label.toUpperCase() === 'B' ? 'selected' : ''}>B</option><option value="C" ${option.label.toUpperCase() === 'C' ? 'selected' : ''}>C</option><option value="D" ${option.label.toUpperCase() === 'D' ? 'selected' : ''}>D</option></select><input required aria-label="Nội dung phương án ${escapeHtml(option.label)}" data-edit-field="option-content" data-index="${index}" data-option-index="${optionIndex}" value="${escapeHtml(option.content)}" /><input type="radio" name="correct-answer-${index}" aria-label="Đáp án đúng ${escapeHtml(option.label)}" data-edit-field="correct-option" data-index="${index}" data-option-index="${optionIndex}" ${option.isCorrect ? 'checked' : ''} /><span>Đáp án đúng</span>${question.options.length > 4 ? `<button class="button-danger option-remove" type="button" data-action="remove-exam-option" data-index="${index}" data-option-index="${optionIndex}" aria-label="Xóa phương án ${escapeHtml(option.label)}">×</button>` : ''}</label>`;
            }).join('')}</div>
            <div class="editor-inline-fields"><label class="field-label">Gợi ý<textarea rows="2" data-edit-field="question-hint" data-index="${index}">${escapeHtml(question.hint)}</textarea></label><label class="field-label">Giải thích<textarea rows="2" data-edit-field="question-explanation" data-index="${index}">${escapeHtml(question.explanation)}</textarea></label></div>
          </article>`).join('') : '<div class="empty-state">Chưa có câu hỏi trong Part này.</div>'}</div>
      </section>
      <div class="editor-footer-actions"><button class="button button-secondary" type="button" data-action="cancel-exam-edit">Hủy</button><button class="button button-primary" type="submit" ${state.examEditSaving ? 'disabled' : ''}>${icon('check')} ${state.examEditSaving ? 'Đang lưu...' : 'Lưu thay đổi'}</button></div>
    </form>`;
}

function createExamEditDraft(detail: AdminExamDetail): AdminExamDetail {
  const draft = JSON.parse(JSON.stringify(detail)) as AdminExamDetail;
  for (const question of draft.questions) {
    const correctCount = question.options.filter((option) => option.isCorrect).length;
    if (correctCount !== 1) {
      question.options.forEach((option) => {
        option.isCorrect = false;
      });
    }
    for (const [index, label] of ['A', 'B', 'C', 'D'].entries()) {
      if (!question.options.some((option) => option.label.trim().toUpperCase() === label)) {
        question.options.push({
          id: 0,
          label,
          content: '',
          orderNumber: index + 1,
          isCorrect: false,
        });
      }
    }
  }
  return draft;
}

function renderExamModal(): string {
  const exam = state.exams.find((item) => item.id === state.editingExamId);
  const editing = Boolean(exam);
  return `
    <div class="modal-backdrop" data-action="close-modal">
      <section class="modal-card" role="dialog" aria-modal="true" aria-labelledby="modal-title">
        <div class="modal-heading"><div><h2 id="modal-title">${editing ? 'Chỉnh sửa đề thi' : 'Tạo đề thi mới'}</h2><p>Nhập thông tin cơ bản cho đề thi mẫu.</p></div><button class="icon-button" type="button" data-action="close-modal" aria-label="Đóng">${icon('close')}</button></div>
        <form id="exam-form">
          <label class="field-label">Tiêu đề đề thi <span class="required">*</span><input name="title" required maxlength="120" placeholder="Ví dụ: TOEIC Reading - Practice Test 02" value="${escapeHtml(exam?.title ?? '')}" /></label>
          <label class="field-label">Thời lượng (phút) <span class="required">*</span><input name="duration" type="number" min="1" max="180" required value="${exam?.duration ?? 75}" /></label>
          <label class="field-label">Mô tả<textarea name="description" rows="3" maxlength="500" placeholder="Mô tả ngắn về nội dung đề thi">${escapeHtml(exam?.description ?? '')}</textarea></label>
          <div class="form-note">${icon('warning')}${usingSampleData ? editing ? 'Chỉnh sửa dữ liệu mẫu trên giao diện; chưa lưu vào SQL Server.' : 'Tạo bản nháp mẫu trong giao diện, không ghi vào SQL Server.' : editing ? 'Cập nhật sẽ được gửi đến backend.' : 'Backend chưa cung cấp API tạo đề thủ công; hãy dùng chức năng Tải đề từ Word.'}</div>
          <div class="modal-actions"><button class="button button-secondary" type="button" data-action="close-modal">Hủy</button><button class="button button-primary" type="submit" ${!usingSampleData && !editing ? 'disabled' : ''}>${icon(editing ? 'check' : 'plus')} ${usingSampleData ? editing ? 'Lưu đề thi mẫu' : 'Tạo bản nháp mẫu' : editing ? 'Lưu đề thi' : 'API chưa hỗ trợ'}</button></div>
        </form>
      </section>
    </div>`;
}

function formatBytes(bytes: number): string {
  return bytes < 1024 * 1024 ? `${Math.max(1, Math.round(bytes / 1024))} KB` : `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
}

function render(): void {
  state.page = currentPage();
  let content = '';
  if (state.page === 'overview') content = renderOverview();
  if (state.page === 'exams') content = renderExams();
  if (state.page === 'upload') content = renderUpload();
  if (state.page === 'questions') content = renderQuestions();
  if (state.page === 'exam-detail') content = renderExamDetail();
  renderLayout(content);
}

function showToast(message: string, tone: 'success' | 'error' = 'success'): void {
  const region = document.querySelector<HTMLDivElement>('#toast-region');
  if (!region) return;
  const toast = document.createElement('div');
  toast.className = `toast ${tone}`;
  toast.innerHTML = `${icon(tone === 'success' ? 'check' : 'warning')}<span>${escapeHtml(message)}</span>`;
  region.append(toast);
  window.setTimeout(() => toast.remove(), 3600);
}

function preserveInputAfterRender(selector: string, caret: number | null): void {
  render();
  const nextInput = document.querySelector<HTMLInputElement>(selector);
  nextInput?.focus();
  if (nextInput && caret !== null) nextInput.setSelectionRange(caret, caret);
}

function selectUploadFile(file: File | undefined): void {
  if (!file) return;
  if (!file.name.toLocaleLowerCase().endsWith('.docx')) {
    state.uploadFile = null;
    state.uploadStatus = 'empty';
    render();
    showToast('Vui lòng chọn tệp có định dạng .docx.', 'error');
    return;
  }
  if (file.size > 10 * 1024 * 1024) {
    state.uploadFile = null;
    state.uploadStatus = 'empty';
    render();
    showToast('Tệp vượt quá giới hạn 10 MB.', 'error');
    return;
  }
  if (file.size === 0) {
    state.uploadFile = null;
    state.uploadStatus = 'empty';
    render();
    showToast('Tệp DOCX rỗng. Vui lòng chọn tệp khác.', 'error');
    return;
  }
  state.uploadFile = file;
  state.uploadStatus = 'ready';
  state.uploadPreview = null;
  state.uploadResult = null;
  state.uploadError = '';
  state.uploadPreviewOpen = false;
  render();
}

async function loadData(): Promise<void> {
  state.loading = true;
  state.error = '';
  state.loginError = '';
  state.requiresAuth = false;
  state.accessDenied = false;
  render();
  try {
    const exams = await api.getExams();
    const [dashboard, activities, examDetails, questions] = await Promise.all([
      api.getDashboard(exams),
      api.getActivities(),
      api.getAdminExamDetails(exams),
      api.getQuestions(exams),
    ]);
    state.dashboard = dashboard;
    state.activities = activities;
    state.exams = exams;
    state.examDetails = Object.fromEntries(
      examDetails.map((detail) => [detail.id, detail]),
    );
    state.questions = questions;
    state.selectedQuestionId = questions[0]?.id ?? '';
  } catch (error) {
    state.error = error instanceof Error ? error.message : 'Lỗi không xác định khi tải dữ liệu.';
    const status = error instanceof ApiClientError ? error.status : 0;
    state.requiresAuth = status === 401 || status === 403;
    state.accessDenied = status === 403;
    if (state.requiresAuth) {
      api.signOut();
    }
    if (state.accessDenied) {
      state.loginError = 'Đăng nhập thành công nhưng tài khoản không được cấp quyền quản trị.';
    }
  } finally {
    state.loading = false;
    render();
  }
}

root.addEventListener('click', async (event) => {
  const target = event.target;
  if (!(target instanceof Element)) return;
  const routeLink = target.closest<HTMLAnchorElement>('a[href^="#/"]');
  if (
    routeLink &&
    state.examEditDraft &&
    JSON.stringify(state.examEditDraft) !== state.examEditOriginal &&
    routeLink.dataset.action !== 'cancel-exam-edit'
  ) {
    if (!window.confirm('Bạn có thay đổi chưa lưu. Rời trang và bỏ các thay đổi này?')) {
      event.preventDefault();
      return;
    }
    state.examEditDraft = null;
    state.examEditOriginal = '';
  }
  const button = target.closest<HTMLElement>('[data-action]');
  if (!button) return;
  const action = button.dataset.action;
  const id = button.dataset.id;

  if (action === 'close-menu') {
    state.mobileMenuOpen = false;
    render();
  } else if (action === 'toggle-menu') {
    state.mobileMenuOpen = !state.mobileMenuOpen;
    render();
  } else if (action === 'open-exam-modal') {
    state.editingExamId = '';
    state.modalOpen = true;
    render();
    document.querySelector<HTMLInputElement>('#exam-form input[name="title"]')?.focus();
  } else if (action === 'close-modal') {
    if ((button.classList.contains('modal-backdrop') && target === button) || !button.classList.contains('modal-backdrop')) {
      state.modalOpen = false;
      state.editingExamId = '';
      render();
    }
  } else if (action === 'exam-page') {
    state.examPage = Number(button.dataset.page) || 1;
    render();
  } else if (action === 'question-page') {
    state.questionPage = Number(button.dataset.page) || 1;
    render();
  } else if (action === 'select-question' && id) {
    state.selectedQuestionId = id;
    render();
  } else if (action === 'reset-exam-filters') {
    state.examSearch = '';
    state.examStatus = 'all';
    state.examPage = 1;
    render();
  } else if (action === 'retry-load') {
    await loadData();
  } else if (action === 'show-login') {
    state.requiresAuth = true;
    state.accessDenied = false;
    state.loginError = '';
    state.error = 'Đăng nhập bằng tài khoản có quyền quản lý đề thi.';
    render();
  } else if (action === 'sign-out') {
    api.signOut();
    state.exams = [];
    state.examDetails = {};
    state.questions = [];
    await loadData();
  } else if (action === 'remove-file') {
    state.uploadFile = null;
    state.uploadStatus = 'empty';
    state.uploadPreview = null;
    state.uploadResult = null;
    state.uploadError = '';
    state.uploadPreviewOpen = false;
    render();
  } else if (action === 'process-upload') {
    if (!state.uploadFile) return;
    if (usingSampleData) {
      state.uploadError =
        'Không thể phân tích hoặc lưu đề thật khi đang bật dữ liệu mẫu. Chuyển VITE_USE_MOCK_DATA=false để dùng API backend.';
      state.uploadStatus = 'error';
      render();
      return;
    }
    state.uploadStatus = 'processing';
    state.uploadError = '';
    state.uploadPreview = null;
    state.uploadResult = null;
    render();
    try {
      state.uploadPreview = await api.parseExamDocument(state.uploadFile);
      state.uploadStatus = 'complete';
      state.uploadPreviewOpen = true;
      render();
      showToast('Đã phân tích file DOCX từ backend.');
    } catch (error) {
      state.uploadStatus = 'error';
      state.uploadError =
        error instanceof Error ? error.message : 'Không thể phân tích file DOCX.';
      render();
      showToast(state.uploadError, 'error');
    }
  } else if (action === 'preview-import') {
    state.uploadPreviewOpen = !state.uploadPreviewOpen;
    render();
  } else if (action === 'save-import') {
    if (!state.uploadPreview || getUploadIssues(state.uploadPreview).length > 0) {
      showToast('Hãy sửa các lỗi và xác nhận đáp án trước khi nhập đề.', 'error');
      return;
    }
    state.uploadStatus = 'importing';
    state.uploadError = '';
    render();
    try {
      state.uploadResult = await api.saveImportedExam(state.uploadPreview);
      state.uploadStatus = 'imported';
      render();
      showToast(`Đã lưu đề #${state.uploadResult.examId} ở trạng thái bản nháp.`);
      try {
        state.exams = await api.getExams();
        state.examPage = 1;
        render();
      } catch (error) {
        showToast(
          `Đề đã lưu thành công nhưng không tải lại được danh sách: ${
            error instanceof Error ? error.message : 'lỗi không xác định.'
          }`,
          'error',
        );
      }
    } catch (error) {
      state.uploadStatus = 'complete';
      state.uploadError =
        error instanceof Error ? error.message : 'Không thể nhập đề vào backend.';
      render();
      showToast(state.uploadError, 'error');
    }
  } else if (action === 'new-question') {
    const id = `DEMO-NEW-${Date.now()}`;
    const question: Question = {
      id,
      part: 'Part 5',
      passageTitle: '',
      passage: '',
      prompt: '',
      options: { A: '', B: '', C: '', D: '' },
      correctAnswer: '',
      hint: '',
      explanation: '',
    };
    state.questions.unshift(question);
    state.selectedQuestionId = id;
    state.newQuestionId = id;
    state.questionSearch = '';
    state.questionPart = 'all';
    state.questionPage = 1;
    render();
    document.querySelector('.question-editor')?.scrollIntoView({ behavior: 'smooth', block: 'start' });
  } else if (action === 'edit-exam') {
    const exam = state.exams.find((item) => item.id === id) ?? currentExam();
    if (!exam) return;
    if (window.location.hash !== `#/exams/${encodeURIComponent(exam.id)}`) {
      window.location.hash = `#/exams/${encodeURIComponent(exam.id)}`;
    }
    if (usingSampleData) {
      showToast('Không thể chỉnh sửa dữ liệu mẫu. Hãy tắt VITE_USE_MOCK_DATA để tải đề từ backend.', 'error');
      return;
    }
    if (exam.status === 'Đã xuất bản') {
      showToast('Hãy tắt xuất bản trước khi chỉnh sửa đề thi.', 'error');
      return;
    }
    try {
      const detail = state.examDetails[exam.id] ?? await api.getAdminExamDetail(exam.id);
      state.examEditDraft = createExamEditDraft(detail);
      state.examEditOriginal = JSON.stringify(state.examEditDraft);
      state.examEditPart = 5;
      render();
    } catch (error) {
      showToast(error instanceof Error ? error.message : 'Không thể tải đề thi cần chỉnh sửa.', 'error');
    }
  } else if (action === 'cancel-exam-edit') {
    if (
      state.examEditDraft &&
      JSON.stringify(state.examEditDraft) !== state.examEditOriginal &&
      !window.confirm('Hủy chỉnh sửa và bỏ các thay đổi chưa lưu?')
    ) {
      return;
    }
    state.examEditDraft = null;
    state.examEditOriginal = '';
    render();
  } else if (action === 'add-exam-passage') {
    const draft = state.examEditDraft;
    if (!draft || state.examEditPart === 5) return;
    const tempId = -Date.now();
    draft.readingPassages.push({
      id: tempId,
      partNumber: state.examEditPart,
      title: '',
      content: '',
      orderNumber:
        Math.max(
          0,
          ...draft.readingPassages
            .filter((passage) => passage.partNumber === state.examEditPart)
            .map((passage) => passage.orderNumber),
        ) + 1,
    });
    render();
  } else if (action === 'remove-exam-passage') {
    const draft = state.examEditDraft;
    const passageIndex = Number(button.dataset.index);
    const passage = draft?.readingPassages[passageIndex];
    if (!draft || !passage) return;
    if (draft.questions.some((question) => question.readingPassageId === passage.id)) {
      showToast('Không thể xóa bài đọc đang được câu hỏi sử dụng.', 'error');
      return;
    }
    if (!window.confirm('Xóa bài đọc này khỏi đề thi?')) return;
    draft.readingPassages.splice(passageIndex, 1);
    render();
  } else if (action === 'add-exam-question') {
    const draft = state.examEditDraft;
    if (!draft) return;
    const part = state.examEditPart;
    const passage = draft.readingPassages.find((item) => item.partNumber === part);
    if (part !== 5 && !passage) {
      showToast(`Hãy thêm bài đọc Part ${part} trước khi thêm câu hỏi.`, 'error');
      return;
    }
    draft.questions.push({
      id: -Date.now(),
      readingPassageId: part === 5 ? null : passage!.id,
      partNumber: part,
      questionNumber:
        Math.max(
          0,
          ...draft.questions
            .filter((question) => question.partNumber === part)
            .map((question) => question.questionNumber),
        ) + 1,
      questionText: '',
      hint: '',
      explanation: '',
      options: ['A', 'B', 'C', 'D'].map((label, index) => ({
        id: 0,
        label,
        content: '',
        orderNumber: index + 1,
        isCorrect: false,
      })),
    });
    render();
  } else if (action === 'remove-exam-question') {
    const draft = state.examEditDraft;
    const questionIndex = Number(button.dataset.index);
    if (!draft?.questions[questionIndex]) return;
    if (!window.confirm('Xóa câu hỏi này khỏi đề thi?')) return;
    draft.questions.splice(questionIndex, 1);
    render();
  } else if (action === 'remove-exam-option') {
    const question = state.examEditDraft?.questions[Number(button.dataset.index)];
    const optionIndex = Number(button.dataset.optionIndex);
    if (!question || question.options.length <= 4) return;
    question.options.splice(optionIndex, 1);
    render();
  } else if (action === 'exam-more' && id) {
    window.location.hash = `#/exams/${encodeURIComponent(id)}`;
  }
});

root.addEventListener('change', async (event) => {
  const target = event.target;
  if (!(target instanceof HTMLInputElement) || target.dataset.action !== 'toggle-publish') {
    return;
  }

  const exam = state.exams.find((item) => item.id === target.dataset.id);
  if (!exam) {
    render();
    return;
  }

  const nextIsPublished = target.checked;
  const nextStatus = nextIsPublished ? 'Đã xuất bản' : 'Bản nháp';
  const actionText = nextIsPublished ? 'kích hoạt và xuất bản' : 'tắt kích hoạt và chuyển về bản nháp';
  if (!window.confirm(`Bạn có chắc muốn ${actionText} đề thi "${exam.title}"?`)) {
    render();
    return;
  }

  state.publicationUpdatingExamId = exam.id;
  render();
  try {
    const savedStatus = await api.setExamPublicationStatus(exam.id, nextIsPublished);
    if (savedStatus !== nextIsPublished) {
      throw new Error('Trạng thái nhận được không khớp với yêu cầu.');
    }
    exam.status = nextStatus;
    const detail = state.examDetails[exam.id];
    if (detail) {
      detail.status = nextStatus;
    }
    state.publicationUpdatingExamId = '';
    render();
    showToast(`Đã lưu trạng thái "${nextStatus}" vào backend.`);
  } catch (error) {
    state.publicationUpdatingExamId = '';
    render();
    showToast(
      error instanceof Error
        ? error.message
        : 'Không thể cập nhật trạng thái kích hoạt đề thi.',
      'error',
    );
  }
});

function updateExamEditorField(target: HTMLInputElement | HTMLTextAreaElement | HTMLSelectElement): void {
  const draft = state.examEditDraft;
  const field = target.dataset.editField;
  if (!draft || !field) return;

  const index = Number(target.dataset.index);
  const passage = draft.readingPassages[index];
  const question = draft.questions[index];
  switch (field) {
    case 'title':
      draft.title = target.value;
      break;
    case 'description':
      draft.description = target.value;
      break;
    case 'duration':
      draft.duration = Number(target.value);
      break;
    case 'passage-title':
      if (passage) passage.title = target.value;
      break;
    case 'passage-content':
      if (passage) passage.content = target.value;
      break;
    case 'passage-order':
      if (passage) passage.orderNumber = Number(target.value);
      break;
    case 'question-part':
      if (question) {
        question.partNumber = Number(target.value);
        const linkedPassage = draft.readingPassages.find(
          (item) => item.id === question.readingPassageId,
        );
        if (question.partNumber === 5 || linkedPassage?.partNumber !== question.partNumber) {
          question.readingPassageId = null;
        }
      }
      break;
    case 'question-number':
      if (question) question.questionNumber = Number(target.value);
      break;
    case 'question-passage':
      if (question) question.readingPassageId = target.value ? Number(target.value) : null;
      break;
    case 'question-text':
      if (question) question.questionText = target.value;
      break;
    case 'question-hint':
      if (question) question.hint = target.value;
      break;
    case 'question-explanation':
      if (question) question.explanation = target.value;
      break;
    case 'option-content': {
      const option = question?.options[Number(target.dataset.optionIndex)];
      if (option) option.content = target.value;
      break;
    }
    case 'option-label': {
      const option = question?.options[Number(target.dataset.optionIndex)];
      if (option) option.label = target.value;
      break;
    }
    case 'correct-option': {
      const correctIndex = Number(target.dataset.optionIndex);
      question?.options.forEach((option, optionIndex) => {
        option.isCorrect = optionIndex === correctIndex;
      });
      break;
    }
  }
}

root.addEventListener('input', (event) => {
  const target = event.target;
  if (
    target instanceof HTMLInputElement ||
    target instanceof HTMLTextAreaElement ||
    target instanceof HTMLSelectElement
  ) {
    updateExamEditorField(target);
  }
});

root.addEventListener('input', (event) => {
  const target = event.target;
  if (!(target instanceof HTMLInputElement)) return;
  const caret = target.selectionStart;
  if (target.dataset.importOption !== undefined && state.uploadPreview) {
    const [questionIndex, label] = target.dataset.importOption.split('-');
    const question = state.uploadPreview.questions[Number(questionIndex)];
    if (!question || !['A', 'B', 'C', 'D'].includes(label)) return;
    const option = question.options.find((item) => item.label.trim().toUpperCase() === label);
    if (option) {
      option.content = target.value;
    } else {
      question.options.push({ label, content: target.value });
    }
    preserveInputAfterRender(
      `input[data-import-option="${target.dataset.importOption}"]`,
      caret,
    );
  } else if (target.dataset.search === 'exams') {
    state.examSearch = target.value;
    state.examPage = 1;
    preserveInputAfterRender('[data-search="exams"]', caret);
  } else if (target.dataset.search === 'questions') {
    state.questionSearch = target.value;
    state.questionPage = 1;
    preserveInputAfterRender('[data-search="questions"]', caret);
  }
});

root.addEventListener('change', (event) => {
  const target = event.target;
  if (!(target instanceof HTMLSelectElement || target instanceof HTMLInputElement)) return;
  if (target.dataset.editPart !== undefined) {
    state.examEditPart = Number(target.value) as 5 | 6 | 7;
    render();
  } else if (target.dataset.editField) {
    updateExamEditorField(target);
    if (target.dataset.editField === 'question-part') {
      render();
    }
  } else if (target instanceof HTMLInputElement && target.dataset.editField === 'correct-option') {
    updateExamEditorField(target);
  } else if (target instanceof HTMLSelectElement && target.dataset.filter === 'exam-status') {
    state.examStatus = target.value as ExamStatusFilter;
    state.examPage = 1;
    render();
  } else if (target instanceof HTMLSelectElement && target.dataset.filter === 'question-part') {
    state.questionPart = target.value;
    state.questionPage = 1;
    render();
  } else if (target instanceof HTMLInputElement && target.dataset.fileInput !== undefined) {
    selectUploadFile(target.files?.[0]);
  } else if (target instanceof HTMLSelectElement && target.dataset.importAnswer !== undefined && state.uploadPreview) {
    const question = state.uploadPreview.questions[Number(target.dataset.importAnswer)];
    if (question) {
      question.answer = target.value;
      render();
    }
  }
});

root.addEventListener('submit', async (event) => {
  const target = event.target;
  if (!(target instanceof HTMLFormElement)) return;
  event.preventDefault();

  if (target.id === 'global-search-form') {
    const query = new FormData(target).get('query')?.toString().trim() ?? '';
    state.examSearch = query;
    state.examPage = 1;
    if (window.location.hash === '#/exams') render();
    else window.location.hash = '#/exams';
  } else if (target.id === 'admin-login') {
    const form = new FormData(target);
    const username = form.get('userNameOrEmailAddress')?.toString().trim() ?? '';
    const password = form.get('password')?.toString() ?? '';
    try {
      await api.authenticate(username, password);
      state.loginError = '';
      state.accessDenied = false;
      await loadData();
    } catch (error) {
      state.requiresAuth = true;
      state.accessDenied = false;
      state.error = 'Đăng nhập không thành công. Kiểm tra tài khoản, mật khẩu và quyền Admin.';
      state.loginError =
        error instanceof Error ? error.message : 'Không thể đăng nhập vào backend.';
      render();
    }
  } else if (target.id === 'exam-editor-form') {
    const draft = state.examEditDraft;
    if (!draft || state.examEditSaving) return;
    if (
      !draft.title.trim() ||
      !Number.isInteger(draft.duration) ||
      draft.duration < 1 ||
      draft.duration > 300
    ) {
      showToast('Hãy nhập tên đề và thời lượng từ 1 đến 300 phút.', 'error');
      return;
    }
    for (const question of draft.questions) {
      if (
        question.partNumber === 5 &&
        question.readingPassageId !== null
      ) {
        showToast(`Câu ${question.questionNumber}: Part 5 không được liên kết bài đọc.`, 'error');
        return;
      }
      if (
        question.partNumber !== 5 &&
        !draft.readingPassages.some(
          (passage) =>
            passage.id === question.readingPassageId &&
            passage.partNumber === question.partNumber,
        )
      ) {
        showToast(`Câu ${question.questionNumber}: hãy chọn bài đọc thuộc đúng Part.`, 'error');
        return;
      }
      if (
        question.options.length !== 4 ||
        question.options.some((option) => !option.content.trim()) ||
        question.options.filter((option) => option.isCorrect).length !== 1 ||
        new Set(question.options.map((option) => option.label.toUpperCase())).size !== 4
      ) {
        showToast(`Câu ${question.questionNumber}: kiểm tra đủ phương án và chọn chính xác một đáp án đúng.`, 'error');
        return;
      }
    }

    state.examEditSaving = true;
    render();
    try {
      const savedDetail = await api.updateAdminExam(draft.id, draft);
      state.examDetails[savedDetail.id] = savedDetail;
      const savedExam: Exam = {
        id: savedDetail.id,
        title: savedDetail.title,
        description: savedDetail.description,
        duration: savedDetail.duration,
        questionCount: savedDetail.questions.length,
        updatedAt: savedDetail.updatedAt,
        status: savedDetail.status,
      };
      state.exams = state.exams.map((exam) => exam.id === savedExam.id ? savedExam : exam);
      state.examEditDraft = null;
      state.examEditOriginal = '';
      state.examEditSaving = false;
      render();
      showToast('Đã lưu và tải lại đề thi từ backend.');
      try {
        state.questions = await api.getQuestions(state.exams);
      } catch (error) {
        showToast(
          `Đề đã được lưu và xác nhận từ backend, nhưng không tải lại được danh sách câu hỏi: ${error instanceof Error ? error.message : 'lỗi không xác định.'}`,
          'error',
        );
      }
    } catch (error) {
      state.examEditSaving = false;
      render();
      showToast(
        error instanceof Error ? error.message : 'Không thể lưu đề thi. Các thay đổi vẫn được giữ trên biểu mẫu.',
        'error',
      );
    }
  } else if (target.id === 'exam-form') {
    const form = new FormData(target);
    const duration = Number(form.get('duration'));
    if (!Number.isInteger(duration) || duration < 1 || duration > 180) {
      showToast('Thời lượng phải từ 1 đến 180 phút.', 'error');
      return;
    }
    try {
      const draft = {
        title: form.get('title')?.toString().trim() ?? '',
        duration,
        description: form.get('description')?.toString().trim() ?? '',
      };
      const existing = state.exams.find((exam) => exam.id === state.editingExamId);
      const exam = existing
        ? await api.updateExam(existing.id, draft, existing)
        : await api.createExam(draft);
      state.exams = existing
        ? state.exams.map((item) => (item.id === exam.id ? exam : item))
        : [exam, ...state.exams];
      state.modalOpen = false;
      state.editingExamId = '';
      state.examPage = 1;
      render();
      showToast(
        usingSampleData
          ? `${existing ? 'Đã cập nhật' : 'Đã tạo'} dữ liệu mẫu trên giao diện; chưa lưu vào SQL Server.`
          : 'Đã lưu đề thi.',
      );
    } catch (error) {
      showToast(error instanceof Error ? error.message : 'Không thể tạo đề thi.', 'error');
    }
  } else if (target.id === 'question-form') {
    const selected = state.questions.find((question) => question.id === state.selectedQuestionId);
    if (!selected) return;
    const form = new FormData(target);
    const draft = {
      part: (form.get('part')?.toString() ?? selected.part) as Question['part'],
      passageTitle: form.get('passageTitle')?.toString().trim() ?? '',
      passage: form.get('passage')?.toString().trim() ?? '',
      prompt: form.get('prompt')?.toString().trim() ?? '',
      options: {
        A: form.get('optionA')?.toString().trim() ?? '',
        B: form.get('optionB')?.toString().trim() ?? '',
        C: form.get('optionC')?.toString().trim() ?? '',
        D: form.get('optionD')?.toString().trim() ?? '',
      },
      correctAnswer: (form.get('correctAnswer')?.toString() ?? '') as Question['correctAnswer'],
      hint: form.get('hint')?.toString().trim() ?? '',
      explanation: form.get('explanation')?.toString().trim() ?? '',
    };
    try {
      const isNew = selected.id === state.newQuestionId;
      const question = isNew
        ? await api.createQuestion(draft)
        : await api.updateQuestion(selected.id, draft);
      state.questions = state.questions.map((item) => (item.id === selected.id ? question : item));
      state.selectedQuestionId = question.id;
      state.newQuestionId = '';
      render();
      showToast(usingSampleData ? 'Đã lưu câu hỏi mẫu trong giao diện; chưa lưu vào SQL Server.' : 'Đã lưu câu hỏi.');
    } catch (error) {
      showToast(error instanceof Error ? error.message : 'Không thể cập nhật câu hỏi.', 'error');
    }
  }
});

root.addEventListener('dragover', (event) => {
  const target = event.target;
  if (target instanceof Element && target.closest('.dropzone')) {
    event.preventDefault();
    target.closest('.dropzone')?.classList.add('dragging');
  }
});

root.addEventListener('dragleave', (event) => {
  const target = event.target;
  if (target instanceof Element) target.closest('.dropzone')?.classList.remove('dragging');
});

root.addEventListener('drop', (event) => {
  const target = event.target;
  if (target instanceof Element && target.closest('.dropzone')) {
    event.preventDefault();
    target.closest('.dropzone')?.classList.remove('dragging');
    selectUploadFile((event as DragEvent).dataTransfer?.files[0]);
  }
});

window.addEventListener('hashchange', () => {
  if (
    state.examEditDraft &&
    JSON.stringify(state.examEditDraft) !== state.examEditOriginal &&
    window.location.hash !== `#/exams/${encodeURIComponent(state.examEditDraft.id)}`
  ) {
    if (!window.confirm('Bạn có thay đổi chưa lưu. Rời trang và bỏ các thay đổi này?')) {
      window.location.hash = `#/exams/${encodeURIComponent(state.examEditDraft.id)}`;
      return;
    }
    state.examEditDraft = null;
    state.examEditOriginal = '';
  }
  state.mobileMenuOpen = false;
  render();
});

window.addEventListener('beforeunload', (event) => {
  if (
    state.examEditDraft &&
    JSON.stringify(state.examEditDraft) !== state.examEditOriginal
  ) {
    event.preventDefault();
    event.returnValue = '';
  }
});

if (!window.location.hash) {
  window.location.hash = '#/overview';
} else {
  render();
}
void loadData();
