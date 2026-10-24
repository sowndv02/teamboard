@TB-021 @identity-service
Feature: Lỗi không lường trước của identity-service
  Là người gọi API
  Tôi muốn nhận lỗi theo định dạng chuẩn của hệ thống mà không lộ chi tiết nội bộ
  Để xử lý lỗi thống nhất và không giúp kẻ tấn công biết cấu trúc bên trong

  @S06 @NFR-003
  Scenario: Lỗi chưa xử lý trả lỗi chuẩn không lộ chi tiết nội bộ
    Given identity-service có một thao tác gây lỗi không lường trước
    When tôi kích hoạt thao tác gây lỗi đó
    Then mã trạng thái là 500
    And lỗi theo định dạng chuẩn có mã "INTERNAL_ERROR" và mã vết
    And lỗi có mã tương quan
    And nội dung lỗi không chứa dấu vết ngăn xếp hay tên máy
