@TB-030 @identity-service @docker
Feature: identity-service user login
  As a registered user
  I want to sign in with email and password
  So that failures do not reveal whether the email exists

  @S01 @FR-002 @TB-032
  Scenario: Valid credentials return 200 with user id and access token without password material
    Given an empty PostgreSQL database is running
    And identity-service access token issuance is configured for BDD tests
    And identity-service is configured to use that database and applies migrations on startup
    And a user is registered with email "login-s01@example.com" and password "ValidPass123456"
    When I send a login request with email "login-s01@example.com" and password "ValidPass123456"
    Then the status code is 200
    And the login response body does not contain "password", "hash", or "salt"
    And the login response has a non-empty id
    And the login response includes a Bearer access token with expires_in

  @S02 @FR-002 @BR-003
  Scenario: Wrong password returns 401 with a stable credential error
    Given an empty PostgreSQL database is running
    And identity-service is configured to use that database and applies migrations on startup
    And a user is registered with email "login-s02@example.com" and password "ValidPass123456"
    When I send a login request with email "login-s02@example.com" and password "WrongPass123456"
    Then the status code is 401
    And the error follows the standard format with code "INVALID_CREDENTIALS" and a trace id
    And the error title is "Invalid email or password."

  @S03 @FR-002 @BR-003
  Scenario: Unknown email returns 401 with the same credential error
    Given an empty PostgreSQL database is running
    And identity-service is configured to use that database and applies migrations on startup
    When I send a login request with email "missing-s03@example.com" and password "ValidPass123456"
    Then the status code is 401
    And the error follows the standard format with code "INVALID_CREDENTIALS" and a trace id
    And the error title is "Invalid email or password."

  @S04 @BR-001
  Scenario: Login accepts normalized email case
    Given an empty PostgreSQL database is running
    And identity-service is configured to use that database and applies migrations on startup
    And a user is registered with email "a@s04.example.com" and password "ValidPass123456"
    When I send a login request with email "A@s04.example.com" and password "ValidPass123456"
    Then the status code is 200
    And the login response has a non-empty id

  @S05 @BR-002
  Scenario: Password shorter than 12 characters is rejected on login
    Given an empty PostgreSQL database is running
    And identity-service is configured to use that database and applies migrations on startup
    When I send a login request with email "short-s05@example.com" and password "short1"
    Then the status code is 400
    And the error follows the standard format with code "VALIDATION_FAILED" and a trace id

  @S06
  Scenario: Invalid email format is rejected on login
    Given an empty PostgreSQL database is running
    And identity-service is configured to use that database and applies migrations on startup
    When I send a login request with email "not-an-email" and password "ValidPass123456"
    Then the status code is 400
    And the error follows the standard format with code "VALIDATION_FAILED" and a trace id

  @S07 @BR-007
  Scenario: Login does not log the submitted password
    Given an empty PostgreSQL database is running
    And identity-service log capture is enabled
    And identity-service is configured to use that database and applies migrations on startup
    And a user is registered with email "log-s07@example.com" and password "ValidPass123456"
    When I send a login request with email "log-s07@example.com" and password "secret-log-s07-password"
    Then the status code is 401
    And no captured log entry contains "secret-log-s07-password"

  @S08 @BR-003
  Scenario: Wrong password and unknown email share code and title
    Given an empty PostgreSQL database is running
    And identity-service is configured to use that database and applies migrations on startup
    And a user is registered with email "shape-s08@example.com" and password "ValidPass123456"
    When I send a login request with email "shape-s08@example.com" and password "WrongPass123456"
    Then the status code is 401
    And the error follows the standard format with code "INVALID_CREDENTIALS" and a trace id
    And the error title is "Invalid email or password."
    When I send a login request with email "unknown-s08@example.com" and password "ValidPass123456"
    Then the status code is 401
    And the error follows the standard format with code "INVALID_CREDENTIALS" and a trace id
    And the error title is "Invalid email or password."

  @S09 @NFR-007
  Scenario: Failed login records login failure metrics
    Given an empty PostgreSQL database is running
    And identity-service metrics are being observed for login failures
    And identity-service is configured to use that database and applies migrations on startup
    When I send a login request with email "metric-s09@example.com" and password "ValidPass123456"
    Then the status code is 401
    And the identity counter "login_failures_total" increased by at least 1