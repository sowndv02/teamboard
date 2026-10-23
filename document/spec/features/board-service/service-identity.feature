@TB-001
Feature: Tên service board-service nhất quán
  Là lập trình viên
  Tôi muốn service có một tên duy nhất ở mọi nơi
  Để các issue sau (gateway, task) tham chiếu đúng tên

  @S01 @FR-001
  Scenario: Tên assembly và namespace gốc của service là board-service
    When tôi kiểm tra assembly của service
    Then tên assembly là "board-service"
    And dự án "src/board-service/board-service.csproj" khai báo namespace gốc "BoardService"

  @S02 @FR-001
  Scenario: Không tệp hiện hành nào còn tên cũ của service
    Given gốc repo được xác định
    When tôi quét mã nguồn, cấu hình và tài liệu hiện hành
    Then không tệp nào chứa tên cũ của service

  @S03 @FR-001 @docker
  Scenario: Compose chạy service board-service ở cổng host 6060
    Given Docker khả dụng
    When tôi chạy compose chính với tên dự án riêng
    Then danh sách service đang chạy gồm "board-service"
    And cổng host 6060 phản hồi một mã HTTP
