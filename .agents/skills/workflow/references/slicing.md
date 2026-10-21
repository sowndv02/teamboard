# Chia lát (slicing): từ spec/design lớn ra các issue nhỏ

Mục đích: một spec lớn không được implement một lần (loãng, dễ sai, PR không review nổi). Sau design `confirmed` và **trước** `plan-init`, chia tính năng thành các **lát giao được**, lưu thành một bảng duy nhất, được người dùng duyệt, rồi mới tạo issue và lập plan **từng lát**. Áp dụng làn **lớn**; làn vừa chỉ cần chia nếu spec có hơn ~3 FR Must hoặc ước lượng vượt vài ngày; làn nhỏ không cần.

Ai chạy: `plan-init` ở chế độ `slices` (bước 4b của `workflow`). `project-manager` (`plan`) dùng cùng bảng để ưu tiên/ước lượng, không tạo bảng thứ hai.

## 1. Tiêu chí một lát tốt

- **Giao được giá trị chạy end-to-end** (lát dọc: một hành vi đi từ API tới DB/sự kiện), không chia theo lớp ("tất cả entity", "tất cả repo").
- **Nhỏ**: hoàn thành trong khoảng 1–3 ngày làm việc, khoảng ≤ 400 dòng diff mỗi PR; lớn hơn thì chia tiếp.
- **Test xanh riêng và merge độc lập**: không để `main` hỏng khi lát đã merge mà lát sau chưa có. Phần chưa muốn lộ ra thì để sau feature flag.
- **Phụ thuộc rõ và ít**: ghi lát nào chặn lát nào; ưu tiên lát "xương sống" (contract, schema, health/observability tối thiểu) đi trước để các lát sau song song được.
- **Truy vết**: mỗi lát trỏ về FR/BR/NFR; mọi FR/NFR Must/Should của spec phải rơi vào ít nhất một lát (không FR mồ côi); NFR xuyên suốt (bảo mật, quan sát) vừa nằm ở lát nền vừa là tiêu chí của mỗi lát.
- **Có tiêu chí hoàn thành kiểm chứng được** (lệnh/kết quả quan sát), nối với tiêu chí chấp nhận Given/When/Then của spec.

Dấu hiệu chia chưa tốt: một lát có hơn ~5 kịch bản chính; một lát chạm hơn một service mà không bắt buộc; tên lát là danh từ chung ("hoàn thiện auth"); lát chỉ "chuẩn bị" mà không chạy được gì.

## 2. File `document/plans/<feature>-slices.md`

```markdown
---
feature: <ten>
spec: document/spec/<ten>.md
design: document/design/<ten>.md
status: draft            # draft | approved | done
updated: YYYY-MM-DD
---

# Slices: <ten>

| # | Lát (hành vi giao được) | FR/NFR | Phụ thuộc | Ước lượng (khoảng + giả định) | Issue | Trạng thái |
|---|---|---|---|---|---|---|
| 1 | Khung + health + đăng ký tối thiểu | FR-001, NFR-02 | – | 1–2 ngày (đã có template service) | TB-0xx | todo |
| 2 | Đăng nhập, cấp token | FR-002 | 1 | 2–3 ngày | TB-0xy | todo |

## Ngoài lát (hoãn/không làm)
## Thứ tự và lý do
## Confirm log
```

Trạng thái lát: `todo` → `planning` → `in-progress` → `review` → `done` (theo issue). Bảng là **điểm resume**: lát đầu tiên chưa `done` là lát hiện tại.

## 3. Quy trình

1. Đọc spec + design `confirmed`; liệt kê FR/BR/NFR và quyết định design; xác định "xương sống".
2. Đề xuất bảng lát theo mục 1; tự kiểm: mọi FR/NFR Must/Should có lát, không lát nào quá lớn, thứ tự theo phụ thuộc rồi giá trị.
3. **Hỏi người dùng** (CONFIRM): duyệt thứ tự/ranh giới lát, lát nào hoãn, ưu tiên khi nhiều lát độc lập. Không tự chốt.
4. Sau duyệt: `status: approved`; đề xuất tạo issue cho từng lát (người dùng đồng ý mới tạo, mỗi issue trỏ về hàng của bảng); ghi số issue vào bảng.
5. **Chỉ lập plan cho lát kế tiếp** (`plan-init` đầu vào = một hàng của bảng). Không lập sẵn plan cho nhiều lát: lát đầu thường làm lộ điều sai ở lát sau. Lát N+1 chỉ bắt đầu plan khi lát N đã merge, hoặc người dùng đồng ý làm song song lát độc lập.
6. Sau mỗi lát xong: cập nhật bảng (trạng thái, ước lượng vs thực tế), xem lại các lát sau còn đúng không. Điều học được làm đổi lát sau → CONFIRM, cập nhật bảng và Confirm log, không sửa im lặng.
7. Spec/design đổi (phạm vi) → bảng bị ảnh hưởng đánh dấu cần xem lại (`workflow` mục đổi phạm vi); lát đã `done` không sửa, tạo lát/issue mới.

## 4. Đạt khi

- Bảng `approved`; mọi FR/NFR Must/Should có lát; không lát nào vượt tiêu chí cỡ mà chưa được người dùng chấp nhận lý do.
- Issue của lát đầu tiên đã tạo (nếu được đồng ý); các lát sau có thể chưa có issue.

## 5. Ngoài đời thực

Nhóm thật gọi việc này là story splitting/slicing và làm cùng đội trong buổi refinement: epic lớn → story nhỏ theo INVEST (độc lập, có giá trị, ước lượng được, nhỏ, kiểm thử được). Họ không lập kế hoạch chi tiết quá xa; lập chi tiết **ngay trước khi làm** và xem lại sau mỗi lát.
