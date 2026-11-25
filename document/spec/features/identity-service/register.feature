@TB-029 @identity-service @docker
Feature: identity-service user registration
  As a new user
  I want to register with an email and password
  So that I can later sign in without exposing secrets in responses or logs

  @S01 @FR-001
  Scenario: Valid email and password return 201 without secrets in the body
    Given an empty PostgreSQL database is running
    And identity-service is configured to use that database and applies migrations on startup
    When I send a registration request with email "new-user-s01@example.com" and password "ValidPass123456"
    Then the status code is 201
    And the registration response body does not contain "password", "hash", or "salt"
    And the registration response has a non-empty id
    And the registration response has a Location header for the created user

  @S02 @FR-001 @BR-003
  Scenario: Duplicate email returns 409 with a stable error code
    Given an empty PostgreSQL database is running
    And identity-service is configured to use that database and applies migrations on startup
    And a user is registered with email "dup-s02@example.com" and password "ValidPass123456"
    When I send a registration request with email "dup-s02@example.com" and password "AnotherPass123456"
    Then the status code is 409
    And the error follows the standard format with code "EMAIL_ALREADY_EXISTS" and a trace id

  @S03 @BR-002
  Scenario: Password shorter than 12 characters is rejected
    Given an empty PostgreSQL database is running
    And identity-service is configured to use that database and applies migrations on startup
    When I send a registration request with email "short-pw-s03@example.com" and password "short1"
    Then the status code is 400
    And the error follows the standard format with code "VALIDATION_FAILED" and a trace id

  @S08 @BR-002
  Scenario: Password longer than 128 characters is rejected
    Given an empty PostgreSQL database is running
    And identity-service is configured to use that database and applies migrations on startup
    When I send a registration request with email "long-pw-s08@example.com" and a password longer than 128 characters
    Then the status code is 400
    And the error follows the standard format with code "VALIDATION_FAILED" and a trace id

  @S11 @FR-001
  Scenario: Invalid email format is rejected
    Given an empty PostgreSQL database is running
    And identity-service is configured to use that database and applies migrations on startup
    When I send a registration request with email "not-an-email" and password "ValidPass123456"
    Then the status code is 400
    And the error follows the standard format with code "VALIDATION_FAILED" and a trace id

  @S07 @BR-001
  Scenario: Email addresses are compared case-insensitively after normalization
    Given an empty PostgreSQL database is running
    And identity-service is configured to use that database and applies migrations on startup
    And a user is registered with email "Case-S07@example.com" and password "ValidPass123456"
    When I send a registration request with email "case-s07@example.com" and password "ValidPass123456"
    Then the status code is 409
    And the error follows the standard format with code "EMAIL_ALREADY_EXISTS" and a trace id

  @S04 @FR-004 @NFR-002
  Scenario: Two accounts with the same password store different password hashes
    Given an empty PostgreSQL database is running
    And identity-service is configured to use that database and applies migrations on startup
    When I register users "hash-a-s04@example.com" and "hash-b-s04@example.com" with the same password "SharedPass123456"
    Then the database stores two different password hashes for those users

  @S05 @FR-004
  Scenario: Registration does not log the submitted password
    Given an empty PostgreSQL database is running
    And identity-service log capture is enabled
    And identity-service is configured to use that database and applies migrations on startup
    When I send a registration request with email "log-s05@example.com" and password "secret-log-s05-password"
    Then the status code is 201
    And no captured log entry contains "secret-log-s05-password"

  @S09 @NFR-007
  Scenario: Successful registration records password hash duration metrics
    Given an empty PostgreSQL database is running
    And identity-service metrics are being observed
    And identity-service is configured to use that database and applies migrations on startup
    When I send a registration request with email "metric-s09@example.com" and password "ValidPass123456"
    Then the identity histogram "password_hash_duration_seconds" recorded at least one sample

  @S10 @FR-001
  Scenario: Concurrent registration for the same new email yields one success and one conflict
    Given an empty PostgreSQL database is running
    And identity-service is configured to use that database and applies migrations on startup
    When two registration requests run concurrently for email "race-s10@example.com" and password "ValidPass123456"
    Then one registration result has status 201 and the other has status 409