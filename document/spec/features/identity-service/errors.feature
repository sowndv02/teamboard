@TB-021 @identity-service
Feature: Unexpected identity-service errors
  As an API caller
  I want to receive errors in the system's standard format without internal details leaking
  So that I can handle errors consistently and an attacker learns nothing about the internal structure

  @S06 @NFR-003
  Scenario: An unhandled error returns the standard error without leaking internal details
    Given identity-service has an operation that causes an unexpected error
    When I trigger that operation
    Then the status code is 500
    And the error follows the standard format with code "INTERNAL_ERROR" and a trace id
    And the error has a correlation id
    And the error body contains no stack trace or machine name
