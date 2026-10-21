# Code review

Lý do tồn tại: review nhất quán, có bằng chứng, không lặp lại finding giữa các vòng và không biến ý tưởng ngoài phạm vi thành blocker. Áp dụng cho mọi agent khi review PR, branch, diff hoặc trước merge.

## Nguyên tắc chung

- **Mặc định report-only**: không tự sửa code trừ khi người dùng yêu cầu rõ ("fix").
- Output tiếng Việt, ngắn gọn; mỗi finding có `file:dòng` và bằng chứng. Phân biệt lỗi chắc chắn với nghi ngờ.
- Đọc trước: `AGENTS.md`, issue liên quan, và rule theo vùng code đổi (`architecture.md`, `dotnet.md`, `messaging.md`, `terraform.md`).
- Phạm vi review là **diff**, nhưng phải truy ra consumer bên ngoài diff khi probe Blast Radius được kích hoạt.
- Không review: `local/**`, file sinh ra (`bin`, `obj`), lockfile (chỉ xem phiên bản đổi). Ghi vào mục Coverage.

## Diff và base

Thứ tự xác định base:
1. Người dùng chỉ định (`base:<ref>`).
2. PR: `gh pr view --json baseRefName`.
3. Mặc định `main` (xem `config/project.yaml` → `branch.baseBranch`).

```bash
BASE=$(git merge-base HEAD origin/main)
git diff -U10 "$BASE"
git status --short        # file untracked: ghi trong Coverage, không review trừ khi được thêm vào diff
```

Report ghi rõ: branch hiện tại, issue (`TB-NNN`), base đã dùng.

## Vòng review (rounds) và tính nhất quán

Trạng thái giữa các vòng lưu trong mục **"Review rounds"** ở cuối file issue (`document/issues/TB-NNN-*.md`), không dựa vào trí nhớ chat.

| Vòng | Phạm vi diff | Yêu cầu |
|---|---|---|
| 1 | `merge-base..HEAD` | **Hoàn chỉnh**: chạy mọi probe áp dụng trước verdict. Không để dành finding cho vòng sau. |
| 2+ | từ commit đã review gần nhất `..HEAD` (delta) | Chỉ probe bị delta kích hoạt; probe không đổi ghi một dòng "Không đổi". |

**Fingerprint** ổn định cho mỗi finding: `<service>:<file-hoặc-symbol>:<concern>` (ví dụ `task-service:OutboxPublisher:publish-before-commit`).

Quy tắc dedup vòng 2+:
1. Đã sửa và có bằng chứng (test/commit) → chuyển sang "Đã xử lý", không liệt kê lại.
2. Chưa sửa, code không đổi → giữ **cùng fingerprint, cùng mức**; không leo thang.
3. P3/Nitpick/Future Enhancement đã nêu vòng 1 → không lặp lại trong Findings.
4. Finding mới ở vòng 2+ phải nêu **vì sao vòng 1 không thấy** (delta mới, hoặc fix gây regression).
5. Một concern = một finding; không tách cùng vấn đề thành nhiều dòng.

Ghi mỗi vòng vào issue:

```
| Vòng | Ngày | Commit đã review | Verdict | Fingerprint mở |
```

## Ba trụ cột (kiểm theo thứ tự trước verdict)

### 1. Correctness — Risk Audit

| # | Probe | Kích hoạt khi | Câu hỏi bắt buộc |
|---|---|---|---|
| F1 | Transaction & lỗi | `try/catch`, transaction, outbox | `catch` có nuốt lỗi DB/hạ tầng không? Lỗi giữa chừng có rollback đúng phạm vi? |
| F2 | Validation | Validation ở endpoint/service/form | Có trùng lặp hoặc mâu thuẫn giữa client, endpoint và domain? Dữ liệu qua validate nhưng hỏng ở mapper thì ai bắt? |
| F3 | Độ sâu test | Test mới/sửa | Test kiểm tra **trạng thái thật** (DB, message phát ra) hay chỉ status code? Có test chuỗi (retry rồi lỗi)? |
| F4 | Idempotency & hết hạn | Consumer, retry, cache, token | Nhận trùng message → tác động một lần? Có test miss/hết hạn? |
| F5 | Đối chiếu chéo | Thêm/sửa rule cho một field | Đã grep mọi nơi cùng kiểu dữ liệu (DTO, entity, form, schema)? |
| F6 | Tiêu chí issue | Issue có tiêu chí hoàn thành | Mỗi tiêu chí đã có evidence (test, lệnh chạy)? Gap quan trọng = finding P1. |
| F7 | Persistence | EF Core, migration | Trường server-managed (`UpdatedAt`, version) đúng? Migration an toàn với dữ liệu cũ? |
| F8 | Bảo mật | Auth, input, secret, session, CORS | AuthZ/IDOR cho mutation? Secret/PII trong log/fixture? Lỗi có lộ chi tiết nội bộ? Token lưu ở đâu? |

### 2. Blast Radius

| # | Probe | Kích hoạt khi | Câu hỏi |
|---|---|---|---|
| B1 | Code dùng chung | `src/shared/`, gateway, base class, interceptor | Đã grep mọi nơi dùng? Consumer khác bị phá không? |
| B2 | Contract downstream | API, event schema, DTO, enum | Service khác (consumer) và contract test đã cập nhật? Breaking change có tăng `version` + ADR? |
| B3 | Tác động chéo | Mutation, outbox, cache, bulk | Service/consumer khác có xung đột (thứ tự, version, quyền)? |
| B4 | Tóm tắt tác động | Luôn | Ghi tác động lên API/contract, database, và tính năng liên quan; không liên quan thì ghi "Không áp dụng" + lý do. |

Probe áp dụng mà chưa grep/trace consumer → **không** được verdict "Sẵn sàng merge".

### 3. Best Practices

| # | Lens | Câu hỏi |
|---|---|---|
| BP1 | Chuẩn dự án | Đúng kiến trúc bắt buộc của service (`architecture.md`)? Không hardcode config, không truy cập DB service khác, `shared/` không có logic nghiệp vụ? |
| BP2 | Idiom ngôn ngữ | C#: async + CancellationToken, nullable, DI, EF không N+1. |
| BP3 | Thiết kế & testability | Trách nhiệm đơn, dependency inject được, port/adapter đúng chỗ? |
| BP4 | Hiệu năng | Có bottleneck thật (query, vòng lặp lớn, bundle) theo quy mô thực tế? Không cảnh báo tối ưu hình thức. |
| BP5 | Dễ đọc | Tên nói rõ ý định, nesting nông, không đòi refactor chỉ vì style. |
| BP6 | Observability | Log có ngữ cảnh (`correlationId`), đúng mức, không `Console.WriteLine`/`console.log`; lỗi giữ stack. |

## Pragmatic guardrails (lọc mọi finding)

- **PG1 Issue/Spec/ADR là ranh giới**: đánh giá theo quyết định đã chọn, không đòi giải pháp khác (ví dụ không đòi Redis khi ADR chọn in-memory).
- **PG2 YAGNI**: không đề xuất abstraction/pattern nặng khi chưa có yêu cầu. Lưu ý: kiến trúc bắt buộc theo service (CQRS/ES, Hexagonal…) **không** bị coi là over-engineering vì là mục tiêu học.
- **PG3 Theo quy mô thực tế**: chỉ nêu hiệu năng khi chạm bottleneck thật.
- **PG4 Cân đối giá trị/chi phí** trước khi đề xuất; chi phí lớn hơn giá trị → bỏ qua hoặc gắn `[Future Enhancement]`.
- **PG5 Dedup vòng sau** (xem trên).
- **PG6 Không workaround**: workaround có sẵn trong phạm vi → finding P1–P2; không tự đề xuất workaround mới (`agent-conduct.md`).
- **PG7 Không giấu rủi ro đã biết** trong phạm vi: phải thành finding hoặc ghi "Giới hạn đã biết"; chưa được người dùng chấp nhận thì không verdict "Sẵn sàng merge".

Nhãn ngoài phạm vi: `[Future Enhancement]` (ý hay nhưng ngoài issue), `[Nitpick]` (style nhỏ, behavior đúng). Cả hai **không chặn merge**. Ngoại lệ: test bám URL/shape sai, so khớp route bằng `endsWith/startsWith`, workaround trong phạm vi, rủi ro tiềm ẩn trong phạm vi → xử lý như P2.

## Gate đi kèm

Trước khi review, kiểm `code-change-gate.md` đã chạy và xanh (build, test, lint, format). Gate fail = finding P1, dừng review chi tiết cho đến khi sửa. Thêm:
- Không `any`, `@ts-ignore`, `#pragma warning disable` mới.
- Không secret trong diff hoặc fixture.
- Đổi behavior/contract thì docs, ADR, test tương ứng đã cập nhật.

## Mức độ

| Mức | Ý nghĩa | Ví dụ |
|---|---|---|
| P0 | Hỏng chức năng, mất dữ liệu, lộ secret | Ghi nhầm DB service khác; secret trong `src/`; bypass auth |
| P1 | Lỗi nghiêm trọng khi dùng thường | Sai ranh giới transaction; publish event ngoài outbox; contract sai; thiếu test lỗi quan trọng |
| P2 | Edge case, rủi ro bảo trì có tác động thật | Hardcode URL/cổng; consumer không idempotent; thiếu cập nhật consumer |
| P3 | Cải thiện nhỏ | Đặt tên, comment, refactor không đổi behavior |

P0–P2 **không** được bỏ qua để merge (chỉ sửa, hoặc người dùng chấp nhận tường minh). P3 còn mở thì **hỏi người dùng** sửa hay bỏ qua (kèm lý do ≥ 10 ký tự); không tự bỏ qua.

## Định dạng báo cáo

Vòng 1 đầy đủ; vòng 2+ thêm mục 3 và rút gọn probe không đổi.

1. **Tóm tắt**: mục đích, branch, issue, base, vòng, commit đã review.
2. **Coverage**: vùng đã review, rule đã đọc, phần bỏ qua + lý do.
3. **So khớp vòng trước** (vòng 2+): bảng `Fingerprint | Vòng trước | Vòng này | Ghi chú`.
4. **Findings**: bảng `Fingerprint | Mức | Vị trí | Vấn đề | Gợi ý`. Vòng 2+ chỉ P0–P2 mới/mở.
   **Đã sửa trong vòng này** (vòng 2+ hoặc sau khi sửa): bảng `Fingerprint | Vị trí | Vấn đề | Đã sửa thế nào | Evidence (commit/test)`; không có thì ghi "Không sửa gì". Nguồn dữ liệu là Fix log của issue (`review-loop.md` quy tắc 12).
5. **Testing gaps**: hành vi chưa có evidence (trong phạm vi delta ở vòng 2+).
6. **Tiêu chí issue (F6)**: bảng `Tiêu chí | Trạng thái | Evidence`.
7. **Risk Audit**: bảng `Probe F1–F8 | Áp dụng? | Kết quả | Finding`; probe áp dụng mà chưa kiểm chứng thì không được verdict merge.
8. **Blast Radius & tác động**: bảng B1–B4 và tóm tắt tác động API/Contract, Database, tính năng liên quan.
9. **Best Practices**: bảng BP1–BP6 `Lens | Áp dụng? | Đạt/Không | Finding`.
10. **Verdict**, một trong:
    - `Sẵn sàng merge`: không còn P0–P2 mở, không P3 `open`, probe áp dụng đã kiểm chứng, không rủi ro tiềm ẩn chưa được chấp nhận.
    - `Sẵn sàng sau khi sửa`: còn P1/P2 có hướng sửa rõ.
    - `Chưa sẵn sàng`: có P0, hoặc P1 chưa có hướng sửa.

Sau báo cáo: cập nhật bảng "Review rounds" trong issue (vòng, commit, verdict, fingerprint mở).

## Khi người dùng yêu cầu sửa finding

- Sửa theo lô nhỏ (≤ 3 finding mỗi commit); mỗi finding có test hoặc evidence trong cùng commit.
- Finding về validation, policy, contract hoặc test coverage chỉ đóng khi có **test mới/mở rộng**, không chỉ sửa code production.
- Ghi từng finding đã sửa vào Fix log của issue và liệt kê trong báo cáo vòng kế tiếp; không sửa im lặng.
- Chạy lại gate, rồi review delta (vòng kế tiếp). Chỉ đóng finding sau khi delta review pass.
- Finding mà fix là hoàn tác thay đổi người dùng đã yêu cầu → hỏi lại người dùng trước khi sửa.

## Bài học từ review

Finding P0–P2 có nguyên nhân hệ thống (quy trình/mẫu/công cụ để lỗi lọt qua, không phải một lần gõ nhầm) → ghi vào báo cáo mục "Bài học đề xuất" và chạy skill `lesson-learned` `capture`; ghi fingerprint finding làm bằng chứng. Finding trùng với lesson đã có và biện pháp phòng ngừa đã `prevented` → đánh dấu `recurred` (biện pháp chưa đủ hiệu quả). Không hỏi lại điều đã có trong `document/lessons/`.
