# Angular reactivity: Signals, RxJS, effect

Lý do tồn tại: hầu hết lỗi tinh vi trong Angular hiện đại đến từ dùng sai Signals/effect/RxJS (vòng lặp cập nhật, rò rỉ subscription, state lệch). Đây là tương đương Angular của "hooks rules". Với zoneless, signal là cách chính để UI biết dữ liệu đổi, nên dùng sai càng dễ thấy hơn (UI không cập nhật). Áp dụng khi viết component/service dùng `signal`, `computed`, `effect`, `resource`, hoặc RxJS trong `src/web/**`.

Bổ sung cho `angular.md` và `frontend-engineering.md`. Tên API `resource`/`httpResource`/Signal Forms thay đổi giữa phiên bản: dùng bản **ổn định** của phiên bản đã chốt trong `config/tool-versions.yaml` (`TODO` tới khi scaffold); không dùng API experimental cho code production.

## 1. Signals

- `signal` cho state có thể ghi; `computed` cho giá trị **dẫn xuất**; `linkedSignal` cho state ghi được nhưng phụ thuộc signal khác (reset khi nguồn đổi).
- `computed` phải **thuần**: không ghi signal, không gọi API, không side effect. Dữ liệu dẫn xuất **không** tạo bằng `effect` + `set`.
- Cập nhật **bất biến**: `update(v => [...v, item])`, `set({ ...v, x })`; không đột biến mảng/đối tượng bên trong signal (OnPush/zoneless và `equal` sẽ không thấy thay đổi).
- Đọc signal bằng `sig()` tại nơi dùng (template, `computed`, `effect`); không cache giá trị `sig()` vào biến dài hạn. Dùng `untracked(() => ...)` khi cố ý đọc mà không muốn theo dõi.
- Không tạo signal/`computed` trong vòng lặp hay hàm gọi lặp lại; khai báo ở field.
- Input/Output: `input()`, `input.required()`, `output()`, `model()`; không `@Input()`/`@Output()` cho code mới.
- Trạng thái dùng chung giữa component: service `providedIn: 'root'` (hoặc theo route) giữ signal chỉ-đọc (`asReadonly()`), thay đổi qua phương thức có tên rõ nghĩa. Thư viện store (NgRx SignalStore...) là quyết định **`TODO`/ADR** khi cần, không thêm sớm (`dependency-policy.md`).

## 2. `effect`

- `effect` **chỉ** cho side effect ra ngoài đồ thị phản ứng: ghi log, đồng bộ `localStorage`, thao tác DOM/thư viện bên thứ ba, phân tích. **Không** dùng để đồng bộ state này sang state kia (dùng `computed`/`linkedSignal`) và **không** gọi API trong `effect` (dùng `resource`/`httpResource` hoặc luồng RxJS).
- Không ghi signal trong `effect` trừ khi thật cần và có comment lý do; dễ gây vòng lặp. Phần đọc không muốn theo dõi bọc bằng `untracked`.
- Dọn tài nguyên bằng `onCleanup`; không để timer/listener rò rỉ.
- `effect` tạo trong injection context (constructor/field initializer); ngoài đó phải truyền `injector`. Cần chạy sau render: `afterNextRender`/`afterRenderEffect`, không `setTimeout`.

## 3. Dữ liệu từ server

- Đọc dữ liệu: `resource`/`rxResource`/`httpResource` (hoặc service RxJS có kiểu) với tham số là **signal/`computed`**; đổi tham số tự huỷ request cũ. Thư viện server-state (TanStack Query cho Angular...) là lựa chọn **`TODO`/ADR 0009**; trước đó dùng cơ chế sẵn có của Angular.
- Trạng thái tải/lỗi/rỗng luôn được xử lý trong UI (`isLoading`, `error`, `value`), không chỉ happy path. Có nút thử lại cho lỗi mạng.
- Ghi dữ liệu (POST/PUT/DELETE): gọi từ handler sự kiện/service, rồi làm mới dữ liệu liên quan có chủ đích (reload resource hoặc cập nhật tại chỗ); không phát API từ `effect`. Cập nhật lạc quan (optimistic) chỉ khi có cách hoàn tác khi lỗi.
- Token và `correlationId`/`traceparent` gắn bằng interceptor chung, không tự gắn từng call.

## 4. RxJS

- **Huỷ subscription**: `takeUntilDestroyed(destroyRef)`, `async` pipe, hoặc `toSignal`. Không `subscribe` không có cơ chế huỷ trong component/service sống lâu.
- Ưu tiên chuyển Observable → signal bằng `toSignal(obs$, { initialValue })` (hoặc `requireSync`); không `subscribe` chỉ để gán vào field.
- **Không subscribe lồng nhau**; dùng toán tử phẳng hoá đúng nghĩa:
  - `switchMap`: chỉ cần kết quả mới nhất (tìm kiếm, đổi tham số).
  - `concatMap`: giữ thứ tự (ghi tuần tự).
  - `exhaustMap`: bỏ qua khi đang chạy (nút gửi).
  - `mergeMap`: song song có chủ đích, kèm giới hạn đồng thời.
- **Lỗi nằm trong luồng**: `catchError` ở luồng **bên trong** `switchMap`/`concatMap` để luồng ngoài không chết; trả giá trị/`EMPTY` có chủ đích, không nuốt lỗi âm thầm (chuyển thành `AppError` + thông báo, `frontend-engineering.md` mục 2).
- `shareReplay({ bufferSize: 1, refCount: true })` khi chia sẻ; không `shareReplay(1)` trần.
- Không dùng `Subject`/`BehaviorSubject` làm kho state mới khi signal đủ dùng; khi dùng, che bằng `asObservable()`.
- Tìm kiếm: `debounceTime` + `distinctUntilChanged` + `switchMap`.
- Không trộn `Promise`/`async-await` vào luồng RxJS nếu không cần; chuyển bằng `firstValueFrom`/`lastValueFrom` có chủ đích, kèm timeout/huỷ.
- Retry chỉ cho thao tác **idempotent** (GET), có backoff và giới hạn; không retry POST không idempotent.

## 5. Cấm tắt rule phản ứng trong code production

- **Cấm** `eslint-disable` (và `@ts-ignore`/`@ts-expect-error` không lý do) cho các rule thuộc: `@angular-eslint/*` (đặc biệt `prefer-signals`, `prefer-inject`, `no-input-rename`), `rxjs-x/*` (`no-ignored-subscription`, `no-nested-subscribe`, `no-unsafe-takeuntil`), `@typescript-eslint/no-floating-promises`, `@typescript-eslint/no-explicit-any` trong `src/web/src/**` (code production).
- Trong test (`*.spec.ts`): được disable khi cần, **kèm issue key (`TB-NNN`) và lý do** ở comment; không disable hàng loạt cả file.
- Cách đúng thay vì disable: sửa nguyên nhân (thêm `takeUntilDestroyed`, đổi sang `computed`, tách hàm).

## 6. Change detection và hiệu năng (zoneless + OnPush)

- `OnPush` bắt buộc; template chỉ đọc signal và giá trị thuần.
- **Zoneless**: thay đổi state ngoài signal (biến thường, callback thư viện bên thứ ba, timer) không kích hoạt render. Đưa vào `signal.set(...)` hoặc `markForCheck` có lý do ghi chú; không đẩy lại `zone.js` để "chạy được".
- Không gọi hàm nặng trong template; dùng `computed` hoặc pipe thuần.
- `@for` luôn có `track` ổn định (id), không `track $index` khi danh sách đổi thứ tự.
- Tránh `ngDoCheck`/`ChangeDetectorRef.detectChanges()` thủ công trừ khi có lý do ghi chú.

## 7. Dependency Injection

- `inject()` trong field initializer/constructor (injection context); không `new` service. Dọn dẹp bằng `DestroyRef`.
- Service dùng chung `providedIn: 'root'`; service gắn vòng đời feature cung cấp ở route/component (`providers`), không để rò rỉ state giữa các màn hình.
- Token inject cho config/hằng dùng `InjectionToken` có kiểu; không đọc `window`/`document` trực tiếp (dùng `DOCUMENT`/`PLATFORM_ID` khi cần, giữ code an toàn nếu sau này bật SSR).

## Ví dụ

```typescript
@Component({ /* standalone, OnPush */ })
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

(Ví dụ minh hoạ nguyên tắc; tên/hình dạng API `resource` có thể khác theo phiên bản đã chốt.)

## Checklist

- [ ] State dẫn xuất dùng `computed`/`linkedSignal`, không `effect` + `set`
- [ ] `effect` chỉ cho side effect ngoài đồ thị; không gọi API trong `effect`
- [ ] Cập nhật signal bất biến; state ngoài signal không "mong zone cập nhật"
- [ ] Mọi subscription có cơ chế huỷ; không subscribe lồng nhau; toán tử phẳng hoá đúng nghĩa
- [ ] Lỗi xử lý trong luồng, không làm chết luồng ngoài, không nuốt âm thầm; retry chỉ cho thao tác idempotent
- [ ] Không `eslint-disable` rule phản ứng trong code production
- [ ] `@for` có `track` ổn định; OnPush; không hàm nặng trong template
