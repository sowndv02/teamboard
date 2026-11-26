@TB-036 @identity-service
Feature: identity-service refresh token rotation
  As a client application
  I want refresh tokens issued on login and rotated on refresh
  So that long-lived sessions can renew access tokens without storing passwords

  Background:
    Given identity-service rate limiting is disabled
    Given identity-service access token issuance is configured for BDD tests

  @S01 @FR-009 @docker
  Scenario: Successful login returns an opaque refresh token
    Given an empty PostgreSQL database is running
    And identity-service refresh token issuance is configured for BDD tests
    And identity-service is configured to use that database and applies migrations on startup
    And a user is registered with email "refresh-s01@example.com" and password "ValidPass123456"
    When I send a login request with email "refresh-s01@example.com" and password "ValidPass123456"
    Then the status code is 200
    And the login response includes an opaque refresh token with refresh_expires_in

  @S02 @FR-009 @docker
  Scenario: Refresh rotates tokens and invalidates the previous refresh token
    Given an empty PostgreSQL database is running
    And identity-service refresh token issuance is configured for BDD tests
    And identity-service is configured to use that database and applies migrations on startup
    And a user is registered with email "refresh-s02@example.com" and password "ValidPass123456"
    When I send a login request with email "refresh-s02@example.com" and password "ValidPass123456"
    Then the status code is 200
    When I store the current login refresh token for rotation checks
    When I send a refresh request with the stored refresh token
    Then the status code is 200
    And the refresh response has no id property
    And the refresh response includes a new access and refresh token pair
    And the refreshed access token differs from the login access token
    When I send a refresh request with the stored refresh token
    Then the status code is 401

  @S03 @FR-009 @docker
  Scenario: Invalid refresh token is rejected
    Given an empty PostgreSQL database is running
    And identity-service refresh token issuance is configured for BDD tests
    And identity-service is configured to use that database and applies migrations on startup
    When I send a refresh request with refresh token "not-a-valid-refresh-token"
    Then the status code is 401

  @S04 @FR-009 @BR-004 @docker
  Scenario: Refresh token lifetime matches configured seconds on login
    Given an empty PostgreSQL database is running
    And identity-service refresh token issuance is configured for BDD tests
    And identity-service is configured to use that database and applies migrations on startup
    And a user is registered with email "refresh-s04@example.com" and password "ValidPass123456"
    When I send a login request with email "refresh-s04@example.com" and password "ValidPass123456"
    Then the status code is 200
    And the login refresh_expires_in is about 120 seconds

  @S05 @FR-009 @docker
  Scenario: Concurrent refresh with the same token allows at most one success
    Given an empty PostgreSQL database is running
    And identity-service refresh token issuance is configured for BDD tests
    And identity-service is configured to use that database and applies migrations on startup
    And a user is registered with email "refresh-s05@example.com" and password "ValidPass123456"
    When I send a login request with email "refresh-s05@example.com" and password "ValidPass123456"
    Then the status code is 200
    When I store the current login refresh token for rotation checks
    When two refresh requests run concurrently with the stored refresh token
    Then exactly one refresh response has status 200
    And exactly one refresh response has status 401