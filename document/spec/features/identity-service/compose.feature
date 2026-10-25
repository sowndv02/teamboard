@TB-021 @identity-service @docker
Feature: Chạy identity-service bằng compose
  Là người phát triển
  Tôi muốn dựng identity-service cùng cơ sở dữ liệu bằng một lệnh compose
  Để chạy và thử service ở máy local giống cách chạy ở môi trường thật

  @S05 @NFR-001
  Scenario: Compose chạy identity-service cùng PostgreSQL và ready đạt trong 30 giây
    Given Docker khả dụng
    And image của identity-service đã được build bằng compose chính
    When tôi chạy identity-service cùng PostgreSQL bằng compose với tên dự án riêng
    Then cổng host 6061 trả 200 cho "/health/ready" trong vòng 30 giây kể từ lệnh chạy

  @S09 @D02
  Scenario: Người dùng cơ sở dữ liệu của identity không có quyền quản trị
    Given Docker khả dụng
    And image của identity-service đã được build bằng compose chính
    When tôi chạy identity-service cùng PostgreSQL bằng compose với tên dự án riêng
    Then người dùng cơ sở dữ liệu của identity không có quyền quản trị

  @S10 @D02
  Scenario: Người dùng cơ sở dữ liệu khác không kết nối được vào database của identity
    Given Docker khả dụng
    And image của identity-service đã được build bằng compose chính
    When tôi chạy identity-service cùng PostgreSQL bằng compose với tên dự án riêng
    Then một người dùng cơ sở dữ liệu khác trong cùng instance không kết nối được vào database của identity
