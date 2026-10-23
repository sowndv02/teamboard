@TB-010 @gateway
Feature: Gateway chạy trong compose
  Là lập trình viên
  Tôi muốn chạy gateway bằng docker compose
  Để thử điểm vào của hệ thống ở máy local

  @S05 @FR-002 @NFR-005 @docker
  Scenario: Compose chạy riêng gateway và gateway sẵn sàng trong 10 giây
    Given Docker khả dụng
    And image của gateway đã được build bằng compose chính
    When tôi chạy riêng service gateway bằng compose với tên dự án riêng
    Then cổng host 8080 trả 200 cho "/health/live"
    And "/health/ready" trả 200 trong vòng 10 giây kể từ lệnh chạy
