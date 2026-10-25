@TB-022 @gateway
Feature: Gateway returns 500 problem+json for unexpected errors
  As an API client
  I want internal errors to always be a uniform problem+json that leaks no details
  So that I can handle errors uniformly and learn nothing about the system internals

  @S05 @NFR-006
  Scenario: An unexpected error returns 500 INTERNAL_ERROR with a correlation id
    Given a fake upstream is running
    And the gateway hits an unexpected error while forwarding
    When I send "GET" to "/api/v1/boards" with X-Correlation-Id "client-id-500"
    Then the response is 500 problem+json with code "INTERNAL_ERROR"
    And the correlationId in the body equals the X-Correlation-Id header of the response
    And the response has exactly one X-Correlation-Id header equal to "client-id-500"
    And the response contains no exception name or stack trace
