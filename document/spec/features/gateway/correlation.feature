@TB-022 @gateway
Feature: Gateway giữ correlation id và truyền trace
  Là người vận hành
  Tôi muốn mỗi request có một correlation id nhất quán từ client qua gateway tới service
  Để lần theo một request khi debug nhiều service mà không tin giá trị client gửi

  Background:
    Given một upstream giả đang chạy
    And gateway được cấu hình trỏ tới upstream giả

  @S01 @FR-003
  Scenario Outline: Id hợp lệ được giữ nguyên, thiếu id thì gateway tạo mới
    When tôi gửi "GET" tới "/api/v1/boards" với X-Correlation-Id <id>
    Then upstream giả và phản hồi nhận cùng đúng một correlation id
    And correlation id đó là <kết quả>

    Examples:
      | id                | kết quả      |
      | "abc-123_X.9"     | "giữ nguyên" |
      | "a*64"            | "giữ nguyên" |
      | (không gửi)       | "GUID mới"   |

  @S02 @FR-003 @NFR-006
  Scenario Outline: Id không hợp lệ bị thay bằng id mới
    When tôi gửi "GET" tới "/api/v1/boards" với X-Correlation-Id <id>
    Then upstream giả và phản hồi nhận cùng đúng một correlation id
    And correlation id đó là "GUID mới"

    Examples:
      | id         |
      | "a b"      |
      | "id;drop"  |
      | "<script>" |
      | "a*65"     |
      | (rỗng)     |
      | "abc\n"    |
      | "١٢٣"      |
      | "a\tb"     |

  @S02 @FR-003
  Scenario: Hai giá trị X-Correlation-Id bị coi là không hợp lệ
    When tôi gửi "GET" tới "/api/v1/boards" với hai header X-Correlation-Id "abc" và "def"
    Then upstream giả và phản hồi nhận cùng đúng một correlation id
    And correlation id đó là "GUID mới"

  @S03 @FR-003
  Scenario: traceparent của client được truyền xuống upstream cùng trace
    When tôi gửi "GET" tới "/api/v1/boards" với traceparent hợp lệ
    Then upstream giả nhận traceparent cùng trace id với client nhưng span id khác

  @S04 @FR-001
  Scenario: 404 mang correlationId bằng header phản hồi
    When tôi gửi "GET" tới "/khong-co-route" với X-Correlation-Id "client-id-404"
    Then phản hồi là 404 problem+json với mã "ROUTE_NOT_FOUND"
    And correlationId trong body bằng header X-Correlation-Id của phản hồi
    And header X-Correlation-Id của phản hồi là "client-id-404"

  @S04 @FR-003
  Scenario: Endpoint health cũng trả X-Correlation-Id
    When tôi gửi "GET" tới "/health/live"
    Then phản hồi có đúng một header X-Correlation-Id là GUID

  @S07 @FR-003
  Scenario: Id do upstream trả không làm phản hồi có hai giá trị
    Given upstream giả luôn trả X-Correlation-Id "from-upstream"
    When tôi gửi "GET" tới "/api/v1/boards" với X-Correlation-Id "client-id-1"
    Then phản hồi có đúng một header X-Correlation-Id là "client-id-1"
