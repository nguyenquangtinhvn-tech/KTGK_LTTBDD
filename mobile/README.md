# TOEIC Reading Student

Flutter Student app for the existing TOEIC Reading ASP.NET Boilerplate API.
The app uses the same SQL Server-backed API as Admin; it does not use a
separate database or sample exam data.

## Run

From this directory:

```powershell
flutter pub get
flutter run --dart-define=API_BASE_URL=https://localhost:44311
```

For an Android emulator or a physical phone, `localhost` is the device itself.
Set `API_BASE_URL` to an HTTPS address reachable from the device, for example
`https://10.0.2.2:44311` when the backend is bound to the emulator host. The
backend certificate must be trusted by the device and valid for that host/IP.
The app intentionally does not bypass TLS certificate validation.

If testing from a physical phone, use the development machine's LAN address and
ensure Kestrel is listening on that interface and has a trusted certificate
whose subject alternative name includes that address. The Windows development
certificate for `localhost` is not automatically valid for a LAN IP.

## Connected endpoints

- `POST /api/TokenAuth/Authenticate` — username/password; returns the ABP JWT.
- `GET /api/services/app/Session/GetCurrentLoginInformations` — current
  account profile and role names.
- `GET /api/services/app/Exam/GetPublishedExams` — published exams.
- `GET /api/services/app/Exam/GetExamDetail?id={id}` — published exam detail,
  passages, questions, and answer option text.
- `POST /api/services/app/StudentExamAttempt/CreateAttempt?examId={id}` —
  creates a server-timed attempt or resumes the student's active attempt for
  the same exam.
- `GET /api/services/app/StudentExamAttempt/GetAttempt?attemptId={id}` —
  resumes an owned attempt with saved selections.
- `POST /api/services/app/StudentExamAttempt/SaveAnswer` — saves or clears one
  option after checking attempt ownership and question membership.
- `POST /api/services/app/StudentExamAttempt/SubmitAttempt?attemptId={id}` —
  grades on the backend and returns a persisted result; repeated submission is
  idempotent.
- `GET /api/services/app/StudentExamAttempt/GetMyAttemptHistory` — lists only
  the authenticated student's completed attempts.

The access token is stored with `flutter_secure_storage`, sent as
`Authorization: Bearer <token>`, and cleared on expiry, 401, or sign-out. The
Student app requires the backend session response to identify the current
account's `Student` role.

## Attempt behavior

- A student can have multiple completed attempts but only one unfinished
  attempt per exam; selecting Start again resumes that active attempt.
- The backend rejects unpublished exams, non-Student accounts, foreign attempt
  IDs, questions outside the attempt's exam, and options outside the selected
  question. Correctness data is never accepted from the client.
- The backend stores the attempt deadline and finalizes expired attempts when
  they are accessed; the mobile timer also requests submission at expiry.
- Score is the raw number of correct answers; no TOEIC scaled-score conversion
  is applied because the existing backend defines no conversion rule.
- Answer keys and explanations are not included in attempt or result responses.
  The existing backend does not define an answer-review permission.
- Additive EF migrations `AddStudentExamAttempts` and
  `AddAttemptQuestionCount` create attempt/answer storage and preserve the
  question count used for grading. Apply later schema changes with the existing
  migrator or EF migration workflow; never reset the database.

## Validation

```powershell
flutter analyze
flutter test
```
