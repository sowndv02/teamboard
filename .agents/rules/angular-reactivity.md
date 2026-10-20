# Angular reactivity: Signals, RxJS, effect

Lý do tồn tại: hầu hết lỗi tinh vi trong Angular hiện đại đến từ dùng sai Signals/effect/RxJS (vòng lặp cập nhật, rò rỉ subscription, state lệch). Đây là tương đương Angular của "hooks rules". Áp dụng khi viết component/service dùng `signal`, `computed`, `effect`, `resource`, hoặc RxJS trong `src/web/**`.

Bổ sung cho `angular.md` (mục State & dữ liệu) và `frontend-engineering.md`.

## 1. Signals

- `signal` cho state có thể ghi; `computed` cho giá trị **dẫn xuất**; `linkedSignal` cho state ghi được nhưng phụ thuộc signal khác (reset khi nguồn đổi).
- `computed` phải **thuần**: không ghi signal, không gọi API, không side effect. Dữ liệu dẫn xuất **không** tạo bằng `effect` + `set`; dùng `computed`/`linkedSignal`.
- Cập nhật **bất biến**: `update(v => [...v, item])`, `set({ ...v, x })`; không đột biến mảng/đối tượng bên trong signal (OnPush và `equal` sẽ không thấy thay đổi).
- Đọc signal bằng `sig()` tại nơi dùng (template, `computed`, `effect`); không cache giá trị `sig()` vào biến dài hạn rồi dùng lại (stale). Dùng `untracked(() => ...)` khi cố ý đọc mà không muốn theo dõi.
- Không tạo signal/`computed` trong vòng lặp hoặc hàm gọi lặp lại mỗi lần change detection; khai báo ở field.
- Input/Output: dùng `input()`, `input.required()`, `output()`, `model()`; không `@Input()` decorator cho code mới.

## 2. `effect`

- `effect` **chỉ** cho side effect ra ngoài đồ thị phản ứng: ghi log, đồng bộ `localStorage`, thao tác DOM/thư viện bên thứ ba, phân tích. **Không** dùng để đồng bộ state này sang state kia (dùng `computed`/`linkedSignal`) và **không** gọi API trong `effect` (dùng `resource`/`rxResource` hoặc luồng RxJS).
- Không ghi signal trong `effect` trừ khi thật cần và được đánh dấu rõ lý do; ghi dễ gây vòng lặp. Nếu buộc, gói bằng `untracked` cho phần đọc không muốn theo dõi.
- Dọn tài nguyên bằng `onCleanup` của `effect`; không để timer/listener rò rỉ.
- `effect` tạo trong injection context (constructor/field initializer); ngoài ngữ cảnh đó phải truyền `injector`.

## 3. Dữ liệu từ server

- Dùng `resource`/`rxResource`/`httpResource` (hoặc service RxJS có kiểu) với tham số là **signal/`computed`**; đổi tham số tự huỷ request cũ. Thư viện server-state (ví dụ TanStack Query cho Angular) là lựa chọn **chưa chốt** (`TODO`, ADR khi chốt); trước đó dùng cơ chế sẵn có của Angular.
- Trạng thái tải/lỗi/rỗng luôn được xử lý trong UI (`isLoading`, `error`, `value`), không chỉ happy path.
- Ghi dữ liệu (POST/PUT/DELETE): gọi từ handler sự kiện/service, rồi làm mới dữ liệu liên quan có chủ đích (reload resource, cập nhật tại chỗ); không tự phát API từ `effect`.
- Truyền `correlationId`/token qua interceptor chung, không tự gắn từng call.

## 4. RxJS

- **Huỷ subscription**: `takeUntilDestroyed(destroyRef)`, `async` pipe, hoặc `toSignal`. Không `subscribe` không có cơ chế huỷ trong component/service sống lâu.
- Ưu tiên chuyển Observable → signal bằng `toSignal(obs$, { initialValue })` (hoặc `requireSync`); không `subscribe` chỉ để gán vào field.
- **Không subscribe lồng nhau**: dùng toán tử phẳng hoá đúng nghĩa:
  - `switchMap`: chỉ cần kết quả mới nhất (tìm kiếm, đổi tham số).
  - `concatMap`: giữ thứ tự (ghi tuần tự).
  - `exhaustMap`: bỏ qua khi đang chạy (nút gửi).
  - `mergeMap`: song song có chủ đích, kèm giới hạn đồng thời.
- **Lỗi nằm trong luồng**: `catchError` ở luồng **bên trong** `switchMap`/`concatMap` để luồng ngoài không chết; trả về giá trị/`EMPTY` có chủ đích, không nuốt lỗi âm thầm (chuyển thành `AppError` + thông báo, `frontend-engineering.md` mục 2).
- `shareReplay({ bufferSize: 1, refCount: true })` khi chia sẻ; không `shareReplay(1)` trần (rò rỉ).
- Không dùng `Subject`/`BehaviorSubject` làm kho state mới khi signal đủ dùng; khi dùng, che bằng `asObservable()`.
- Debounce/throttle tìm kiếm bằng `debounceTime` + `distinctUntilChanged` + `switchMap`.
- Không `Promise`/`async-await` lẫn trong luồng RxJS nếu không cần; chuyển bằng `firstValueFrom`/`lastValueFrom` có chủ đích và kèm timeout/huỷ.

## 5. Cấm tắt rule phản ứng trong code production

- **Cấm** `eslint-disable` (và `@ts-ignore`/`@ts-expect-error` không kèm lý do) cho các rule thuộc: `@angular-eslint/*` (đặc biệt `no-input-rename`, `prefer-signals`, `prefer-inject`), `rxjs/*` (`no-ignored-subscription`, `no-nested-subscribe`, `no-unsafe-takeuntil`), `@typescript-eslint/no-floating-promises`, `@typescript-eslint/no-explicit-any` trong `src/web/src/**` (code production).
- Trong test (`*.spec.ts`): được disable khi cần, **kèm issue key (`TB-NNN`) và lý do** ở comment; không disable hàng loạt cả file.
- Cách đúng thay vì disable: sửa nguyên nhân (thêm `takeUntilDestroyed`, đổi sang `computed`, tách hàm).

## 6. Change detection và hiệu năng

- `ChangeDetectionStrategy.OnPush` bắt buộc; template chỉ đọc signal và giá trị thuần.
- Không gọi hàm nặng trong template; dùng `computed` hoặc pipe thuần.
- `@for` luôn có `track` ổn định (id), không `track $index` khi danh sách thay đổi thứ tự.
- Danh sách lớn: phân trang phía server (`api-endpoints.md` mục 4) hoặc virtual scroll; không render hàng nghìn phần tử.
- Tránh `ngDoCheck`/`ChangeDetectorRef.detectChanges()` thủ công trừ khi có lý do ghi chú.

## 7. Dependency Injection

- Dùng `inject()` trong field initializer/constructor (injection context); không `new` service. Dọn dẹp bằng `DestroyRef`.
- Service dùng chung `providedIn: 'root'`; service gắn vòng đời feature cung cấp ở route/component (`providers`), không để rò rỉ state giữa các màn hình.

## Ví dụ

```typescript
@Component({ /* ... OnPush, standalone */ })
export class TaskListComponent {
  private readonly api = inject(TaskApi);

  readonly boardId = input.required<string>();
  readonly page = signal(1);

  // Tham số là computed; đổi boardId/page thì request cũ bị huỷ tự động
  readonly tasks = rxResource({
    params: computed(() => ({ boardId: this.boardId(), page: this.page() })),
    stream: ({ params }) => this.api.list(params.boardId, params.page),
  });

  readonly isEmpty = computed(() => (this.tasks.value()?.items.length ?? 0) === 0);

  nextPage(): void {
    this.page.update(p => p + 1);
  }
}
```

(Ví dụ minh hoạ nguyên tắc; tên API `resource` thay đổi giữa phiên bản Angular, kiểm tra theo phiên bản đã chốt trong `config/tool-versions.yaml`.)

## Checklist

- [ ] State dẫn xuất dùng `computed`/`linkedSignal`, không `effect` + `set`
- [ ] `effect` chỉ cho side effect ngoài đồ thị; không gọi API trong `effect`
- [ ] Cập nhật signal bất biến
- [ ] Mọi subscription có cơ chế huỷ; không subscribe lồng nhau; toán tử phẳng hoá đúng nghĩa
- [ ] Lỗi xử lý trong luồng, không làm chết luồng ngoài, không nuốt âm thầm
- [ ] Không `eslint-disable` rule phản ứng trong code production
- [ ] `@for` có `track` ổn định; OnPush; không hàm nặng trong template
