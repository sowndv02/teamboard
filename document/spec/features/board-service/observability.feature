@TB-001
Feature: Trace của board-service hiện trong Jaeger
  Là lập trình viên
  Tôi muốn trace của request tới board-service hiện trong Jaeger
  Để debug nhiều service

  @S11 @FR-003 @NFR-005 @docker
  Scenario: Request tới board-service hiện thành trace trong Jaeger
    Given Docker khả dụng
    And compose chính và compose observability đang chạy với tên dự án riêng
    When tôi gọi "GET /health/live" tới cổng host 6060
    Then Jaeger có trace của service "board-service" trong thời gian chờ
    And Jaeger và cổng OTLP chỉ bind 127.0.0.1
