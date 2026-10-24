@TB-010 @gateway
Feature: Gateway chuyển tiếp API boards tới board-service
  Là client
  Tôi muốn gọi /api/v1/boards qua gateway
  Để không cần biết địa chỉ của board-service

  @S01 @FR-001 @BR-001 @NFR-004
  Scenario Outline: Request tới API boards được chuyển tới upstream với đúng method và đường dẫn
    Given một upstream giả đang chạy
    And gateway được cấu hình trỏ tới upstream giả
    When tôi gửi "<method>" tới "<path>"
    Then upstream giả nhận đúng method "<method>" và đường dẫn "<path>"
    And phản hồi của upstream được trả nguyên về client

    Examples:
      | method | path |
      | GET    | /api/v1/boards                         |
      | GET    | /api/v1/boards/3f2b1c4e                |
      | GET    | /api/v1/boards?page=2&pageSize=20      |
      | HEAD   | /api/v1/boards                         |
      | POST   | /api/v1/boards                         |
      | PUT    | /api/v1/boards/3f2b1c4e                |
      | PATCH  | /api/v1/boards/3f2b1c4e                |
      | DELETE | /api/v1/boards/3f2b1c4e                |

  @S02 @FR-001
  Scenario Outline: Request không khớp route nhận 404 problem+json và không tới upstream
    Given một upstream giả đang chạy
    And gateway được cấu hình trỏ tới upstream giả
    When tôi gửi "<method>" tới "<path>"
    Then phản hồi là 404 problem+json với mã "ROUTE_NOT_FOUND"
    And phản hồi có type và title theo mã lỗi của gateway
    And phản hồi có traceId và correlationId
    And phản hồi không chứa địa chỉ hay tên nội bộ của upstream
    And upstream giả không nhận request nào

    Examples:
      | method | path |
      | GET    | /api/v1/unknown     |
      | GET    | /api/v1/boardsX     |
      | GET    | /foo.txt            |
      | TRACE  | /api/v1/boards      |

  @S06 @FR-001
  Scenario: Lỗi do upstream trả về được chuyển nguyên, không bị thay bằng lỗi của gateway
    Given một upstream giả đang chạy và trả 404 problem+json với mã "BOARD_NOT_FOUND" cho đường dẫn "/api/v1/boards/3f2b1c4e"
    And gateway được cấu hình trỏ tới upstream giả
    When tôi gửi "GET" tới "/api/v1/boards/3f2b1c4e"
    Then phản hồi là 404 problem+json với mã "BOARD_NOT_FOUND"
    And phản hồi giữ nguyên nội dung do upstream trả
