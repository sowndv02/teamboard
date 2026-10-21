---
name: angular-feature
description: Dựng một feature Angular theo rule dự án (cấu trúc feature-based, signals, test trước, a11y, i18n, gate frontend) từ plan đã duyệt đến khi gate xanh và đã xem trên trình duyệt. Dùng khi issue/lát chạm src/web hoặc khi bắt đầu một màn hình/luồng UI mới, bên trong bước coding-start của workflow.
---

# Angular feature

Quy trình làm **một lát giao được** có phần Angular, theo vòng Đỏ → Xanh → Refactor của `coding-start`, cộng các bước riêng của frontend. Skill này không thay `workflow`: nó chi tiết hoá cách làm khi diff chạm `src/web/**`.

Rule liên quan: `angular.md`, `angular-reactivity.md`, `frontend-engineering.md`, `frontend-testing.md`, `api-endpoints.md`, `quality-gate.md`, `code-change-gate.md`, `dependency-pinning.md`, `agent-conduct.md`. Review sau khi xong: skill `frontend-review`.

## Đầu vào và điều kiện
- Plan `approved` cho lát (`plan-init`), spec/design `confirmed`; ADR 0009 (FE) đã `accepted`. Chưa → dừng, hỏi người dùng.
- `src/web` đã scaffold và `config/tool-versions.yaml` có `node`, `angular` (không còn `TODO`). Chưa scaffold → **không** tự dựng project trong lát feature; đề xuất issue scaffold riêng.
- Plan là **lát dọc xuyên FE + API**: hành vi người dùng thấy đi từ UI tới API. Lát chỉ có FE mà API chưa có → dùng kiểu sinh từ OpenAPI và mock bám schema, ghi rõ trong plan; không bịa hình dạng API.

## Quy trình
1. **Đọc**: spec/plan/`.feature` của lát, OpenAPI của endpoint liên quan, rule Angular/frontend, lesson có tag `frontend`/`angular`/`security`.
2. **Cập nhật kiểu API**: sinh lại từ OpenAPI (`npm --prefix src/web run openapi:check` phải sạch). Không tự viết tay model trùng contract.
3. **Test trước (Đỏ)**: theo `frontend-testing.md`: unit cho logic/service, component test theo hành vi (selector theo role/label), kịch bản BDD cho luồng người dùng thấy (Playwright khi plan yêu cầu), kiểm axe cho màn hình mới. Chạy và xác nhận đỏ **đúng lý do**.
4. **Cấu trúc**: `src/app/features/<feature>/` (`pages`, `components`, `data-access`, `models`); dùng `core/` cho auth/interceptor/config, `shared/` cho thứ dùng lại **không** chứa nghiệp vụ. Feature không import feature khác (lint bắt).
5. **Xanh (code tối thiểu)**: standalone + `OnPush`, state bằng signals, dữ liệu server qua `resource`/`httpResource` hoặc RxJS có kiểu, template control flow mới, form có kiểu, route lazy + guard mặc định đóng. Chữ hiển thị qua khoá i18n; lỗi qua `AppError` theo `code`; không URL/quyền/route inline.
6. **Refactor** giữ xanh. Không thêm tính năng ngoài plan.
7. **Gate frontend** (từng bước nhỏ chạy targeted; trước khi báo xong chạy đủ): 
   ```bash
   npm ci                                           # gate dùng ci, không install
   npm --prefix src/web run lint                    # --max-warnings=0
   npm --prefix src/web run format:check
   npm --prefix src/web run typecheck
   npm --prefix src/web run test                    # gồm policy test
   npm --prefix src/web run build                   # bundle budgets
   npm --prefix src/web run openapi:check
   npm --prefix src/web audit --omit=dev --audit-level=high
   ```
   Policy test phải xanh: `no-hardcode-policy`, `error-code-policy`, `i18n-keys-complete`, `no-mock-in-prod-bundle`. Axe: 0 vi phạm serious/critical. `e2e` (Playwright smoke) chạy ở mốc full/PR.
8. **Xem trên trình duyệt** (không báo "UI xong" chỉ vì build xanh): golden path, một đường lỗi (mạng/401/validation), bàn phím (Tab/Enter/Esc), thu nhỏ màn hình, Web Vitals sơ bộ. Ghi quan sát vào nhật ký issue. Không có trình duyệt hoặc backend chạy được → nói rõ phần chưa kiểm chứng.
9. **Ghi nhật ký** vào issue (TDD log, ghi chú) và báo `workflow` mở `coding-review` (sẽ gọi `frontend-review`).

## Cấu trúc tham chiếu
```
src/app/
  core/        auth, http interceptors, config, i18n, errors, layout
  shared/      ui components/pipes/directives dùng lại (không nghiệp vụ)
  features/<feature>/
    pages/ components/ data-access/ models/ <feature>.routes.ts
```

## Khi gặp điều chưa rõ (không tự quyết)
Hỏi người dùng khi: UI/UX spec không nói (nội dung, thông báo lỗi, thứ tự, trạng thái rỗng); cần thêm thư viện (hạng T1+, `dependency-policy.md`); đổi nơi lưu token, render strategy (SPA/SSR), i18n tool, thư viện server-state (đều là quyết định ADR); cần sửa contract API. Hành vi chốt xong → cập nhật spec (và plan/`.feature`) **trước**, rồi code.

## Làn (`../workflow/references/lanes.md`)
Đổi contract/auth hoặc xuyên FE + API = làn **lớn**. Sửa giao diện nhỏ không đổi hành vi (văn bản, style, a11y) có thể làn **nhỏ** nhưng vẫn chạy đủ gate FE.

## Ngoài đời thực
Đội FE thật làm theo thiết kế (Figma) và design system, có CI chạy lint/test/build/e2e và preview deploy cho mỗi PR, reviewer kiểm cả hành vi trên trình duyệt. Ở đây người dùng đóng vai khách/reviewer; hãy luyện: contract từ OpenAPI, test hành vi thay vì chi tiết cài đặt, a11y và i18n từ đầu thay vì vá sau.

## Không làm
- Không scaffold project, đổi `angular.json`/`tsconfig*`/eslint config/`package.json` ngoài phạm vi plan.
- Không `any`, `@ts-ignore`, `eslint-disable`, `bypassSecurityTrust*`, `innerHTML` với dữ liệu người dùng, secret/PII trong bundle hoặc log.
- Không `npm install` trong gate; không nới budget, skip test, tắt rule để qua.
- Không tự chốt UX/thư viện/kiến trúc chưa có ADR hay spec.

## Hoàn thành khi
- [ ] Mọi test của lát xanh (gồm policy test, axe), build đạt budgets, `openapi:check` sạch
- [ ] Gate frontend đầy đủ xanh (hoặc nêu rõ phần không chạy được và vì sao)
- [ ] Đã xem trên trình duyệt: golden path, đường lỗi, bàn phím; ghi vào issue
- [ ] Chuỗi hiển thị qua i18n, lỗi qua `AppError`, không hardcode; không thay đổi ngoài phạm vi
- Báo `workflow` mở `coding-review` (gọi `frontend-review`).

## Ghi trạng thái workflow

Theo `../workflow/references/state-protocol.md` (làn lớn bắt buộc; làn vừa/nhỏ ghi vào bảng Workflow của issue): ghi trước khi bắt đầu, sau mỗi vòng/khi cần người dùng chốt/khi bị chặn, và khi hoàn thành (bằng chứng: lệnh gate + kết quả), rồi cập nhật bảng Workflow trong issue.
