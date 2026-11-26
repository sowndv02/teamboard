@TB-039 @identity-service
Feature: identity-service change password revokes refresh tokens
  As an authenticated user
  I want to change my password with my current password
  So that my account is updated and other sessions lose refresh access

  Background:
    Given identity-service rate limiting is disabled
    And identity-service access token issuance is configured for BDD tests

  @S01 @FR-022 @docker
  Scenario: Successful change password returns no content and revokes all refresh sessions
    Given an empty PostgreSQL database is running
    And identity-service refresh token issuance is configured for BDD tests
    And identity-service is configured to use that database and applies migrations on startup
    And a user is registered with email "changepw-s01@example.com" and password "ValidPass123456"
    When I send a login request with email "changepw-s01@example.com" and password "ValidPass123456"
    Then the status code is 200
    When I store the current login refresh token as the first session refresh token
    And I store the current login access token for change password requests
    When I send a login request with email "changepw-s01@example.com" and password "ValidPass123456"
    Then the status code is 200
    When I store the current login refresh token as the second session refresh token
    When I send a change password request with current "ValidPass123456" and new "NewValidPass12345" using the stored change password access token
    Then the status code is 204
    When I send a refresh request with the first session refresh token
    Then the status code is 401
    When I send a refresh request with the second session refresh token
    Then the status code is 401

  @S02 @FR-022 @BR-003 @docker
  Scenario: Wrong current password returns a generic credential error
    Given an empty PostgreSQL database is running
    And identity-service refresh token issuance is configured for BDD tests
    And identity-service is configured to use that database and applies migrations on startup
    And a user is registered with email "changepw-s02@example.com" and password "ValidPass123456"
    When I send a login request with email "changepw-s02@example.com" and password "ValidPass123456"
    Then the status code is 200
    When I store the current login access token for change password requests
    When I send a change password request with current "WrongPass123456" and new "NewValidPass12345" using the stored change password access token
    Then the status code is 401
    And the error follows the standard format with code "INVALID_CREDENTIALS" and a trace id

  @S03 @BR-002 @docker
  Scenario: New password shorter than 12 characters is rejected
    Given an empty PostgreSQL database is running
    And identity-service refresh token issuance is configured for BDD tests
    And identity-service is configured to use that database and applies migrations on startup
    And a user is registered with email "changepw-s03@example.com" and password "ValidPass123456"
    When I send a login request with email "changepw-s03@example.com" and password "ValidPass123456"
    Then the status code is 200
    When I store the current login access token for change password requests
    When I send a change password request with current "ValidPass123456" and new "short1" using the stored change password access token
    Then the status code is 400
    And the error follows the standard format with code "VALIDATION_FAILED" and a trace id

  @S03b @BR-002 @docker
  Scenario: New password must differ from the current password
    Given an empty PostgreSQL database is running
    And identity-service refresh token issuance is configured for BDD tests
    And identity-service is configured to use that database and applies migrations on startup
    And a user is registered with email "changepw-s03b@example.com" and password "ValidPass123456"
    When I send a login request with email "changepw-s03b@example.com" and password "ValidPass123456"
    Then the status code is 200
    When I store the current login access token for change password requests
    When I send a change password request with current "ValidPass123456" and new "ValidPass123456" using the stored change password access token
    Then the status code is 400
    And the error follows the standard format with code "VALIDATION_FAILED" and a trace id

  @S04 @docker
  Scenario: Change password without bearer token is unauthorized
    Given an empty PostgreSQL database is running
    And identity-service refresh token issuance is configured for BDD tests
    And identity-service is configured to use that database and applies migrations on startup
    When I send a change password request with current "ValidPass123456" and new "NewValidPass12345" without an access token
    Then the status code is 401

  @S04 @docker
  Scenario: Change password with an invalid bearer token is unauthorized
    Given an empty PostgreSQL database is running
    And identity-service refresh token issuance is configured for BDD tests
    And identity-service is configured to use that database and applies migrations on startup
    When I send a change password request with current "ValidPass123456" and new "NewValidPass12345" using access token "not-a-valid-jwt"
    Then the status code is 401

  @S05 @BR-006 @docker
  Scenario: Access token remains usable until expiration after password change
    Given an empty PostgreSQL database is running
    And identity-service refresh token issuance is configured for BDD tests
    And identity-service is configured to use that database and applies migrations on startup
    And a user is registered with email "changepw-s05@example.com" and password "ValidPass123456"
    When I send a login request with email "changepw-s05@example.com" and password "ValidPass123456"
    Then the status code is 200
    When I store the current login access token for change password requests
    When I send a change password request with current "ValidPass123456" and new "NewValidPass12345" using the stored change password access token
    Then the status code is 204
    When I send a change password request with current "NewValidPass12345" and new "AnotherValid1234" using the stored change password access token
    Then the status code is 204

  @S06 @FR-003 @docker
  Scenario: Five wrong current passwords lock the user for change password
    Given an empty PostgreSQL database is running
    And identity-service lockout is configured with max failures 5 and lockout duration 60 seconds
    And identity-service refresh token issuance is configured for BDD tests
    And identity-service is configured to use that database and applies migrations on startup
    And a user is registered with email "changepw-s06@example.com" and password "ValidPass123456"
    When I send a login request with email "changepw-s06@example.com" and password "ValidPass123456"
    Then the status code is 200
    When I store the current login access token for change password requests
    When I send 5 change password requests with current "WrongPass123456" and new "NewValidPass12345" using the stored change password access token
    When I send a change password request with current "ValidPass123456" and new "NewValidPass12345" using the stored change password access token
    Then the status code is 401
    And the error follows the standard format with code "INVALID_CREDENTIALS" and a trace id
    And the response has a Retry-After header greater than 0