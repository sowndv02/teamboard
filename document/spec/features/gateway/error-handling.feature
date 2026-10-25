@TB-022 @gateway
Feature: Gateway trả 500 problem+json cho lỗi chưa lường
  Là client của API
  Tôi muốn lỗi nội bộ luôn có dạng problem+json thống nhất và không lộ chi tiết
  Để xử lý lỗi đồng nhất và không biết được cấu trúc bên trong hệ thống

  @S05 @NFR-006
  Scenario: Lỗi chưa lường trả 500 INTERNAL_ERROR kèm correlation id
    Given một upstream giả đang chạy
    And gateway có lỗi chưa lường khi chuyển tiếp
    When tôi gửi "GET" tới "/api/v1/boards" với X-Correlation-Id "client-id-500"
    Then phản hồi là 500 problem+json với mã "INTERNAL_ERROR"
    And correlationId trong body bằng header X-Correlation-Id của phản hồi
    And header X-Correlation-Id của phản hồi là "client-id-500"
    And phản hồi không chứa tên exception hay stack trace
