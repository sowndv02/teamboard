@TB-031 @identity-service @docker
Feature: identity-service login lockout and auth rate limiting
  As a platform operator
  I want temporary lockouts and optional rate limits on auth endpoints
  So that credential stuffing is slowed without revealing account state

  @S01 @FR-003
  Scenario: Five consecutive failures lock the email and IP pair even for correct password
    Given an empty PostgreSQL database is running
    And identity-service lockout is configured with max failures 5 and lockout duration 60 seconds
    And identity-service is configured to use that database and applies migrations on startup
    And a user is registered with email "lock-s01@example.com" and password "ValidPass123456"
    When I send 5 login requests with email "lock-s01@example.com" and password "WrongPass123456"
    And I send a login request with email "lock-s01@example.com" and password "ValidPass123456"
    Then the status code is 401
    And the error follows the standard format with code "INVALID_CREDENTIALS" and a trace id
    And the response has a Retry-After header greater than 0

  @S02 @FR-003 @BR-003
  Scenario: Locked pair returns the same credential error with Retry-After
    Given an empty PostgreSQL database is running
    And identity-service lockout is configured with max failures 5 and lockout duration 60 seconds
    And identity-service is configured to use that database and applies migrations on startup
    And a user is registered with email "lock-s02@example.com" and password "ValidPass123456"
    And the email and IP pair for "lock-s02@example.com" is locked from failed logins
    When I send a login request with email "lock-s02@example.com" and password "WrongPass123456"
    Then the status code is 401
    And the error follows the standard format with code "INVALID_CREDENTIALS" and a trace id
    And the response has a Retry-After header greater than 0

  @S03 @FR-003
  Scenario: Successful login resets the failure counter for the pair
    Given an empty PostgreSQL database is running
    And identity-service lockout is configured with max failures 5 and lockout duration 60 seconds
    And identity-service is configured to use that database and applies migrations on startup
    And a user is registered with email "lock-s03@example.com" and password "ValidPass123456"
    When I send 3 login requests with email "lock-s03@example.com" and password "WrongPass123456"
    And I send a login request with email "lock-s03@example.com" and password "ValidPass123456"
    Then the status code is 200
    When I send 4 login requests with email "lock-s03@example.com" and password "WrongPass123456"
    Then the status code is 401
    And the response has no Retry-After header
    When I send 5 login requests with email "lock-s03@example.com" and password "WrongPass123456"
    Then the status code is 401
    And the response has a Retry-After header greater than 0

  @S04 @FR-003
  Scenario: Lockout for one IP does not block the same email from another IP
    Given an empty PostgreSQL database is running
    And identity-service lockout is configured with max failures 5 and lockout duration 60 seconds
    And identity-service is configured to use that database and applies migrations on startup
    And a user is registered with email "lock-s04@example.com" and password "ValidPass123456"
    And client IP "203.0.113.10" has locked the pair for email "lock-s04@example.com"
    When I send a login request with email "lock-s04@example.com" and password "ValidPass123456" from client IP "203.0.113.10"
    Then the status code is 401
    And the response has a Retry-After header greater than 0
    When I send a login request with email "lock-s04@example.com" and password "ValidPass123456" from client IP "203.0.113.11"
    Then the status code is 200

  @S05 @FR-003
  Scenario: Login succeeds after the lockout duration elapses
    Given an empty PostgreSQL database is running
    And identity-service lockout is configured with max failures 5 and lockout duration 2 seconds
    And identity-service is configured to use that database and applies migrations on startup
    And a user is registered with email "lock-s05@example.com" and password "ValidPass123456"
    And the email and IP pair for "lock-s05@example.com" is locked from failed logins
    When I retry login with email "lock-s05@example.com" and password "ValidPass123456" until success within 10 seconds
    Then the status code is 200

  @S06 @BR-003
  Scenario: Lockout response does not reveal lock state in the body
    Given an empty PostgreSQL database is running
    And identity-service lockout is configured with max failures 5 and lockout duration 60 seconds
    And identity-service is configured to use that database and applies migrations on startup
    And a user is registered with email "lock-s06@example.com" and password "ValidPass123456"
    And the email and IP pair for "lock-s06@example.com" is locked from failed logins
    When I send a login request with email "lock-s06@example.com" and password "ValidPass123456"
    Then the status code is 401
    And the response body does not contain the word "lock"

  @S07 @NFR-008
  Scenario: Enabled rate limiting returns 429 when auth requests exceed the permit limit
    Given an empty PostgreSQL database is running
    And identity-service rate limiting is enabled with permit limit 10 per minute
    And identity-service is configured to use that database and applies migrations on startup
  # Unknown emails fail fast enough for an integration count without registering a user.
    When I send 11 login requests with email "rate-s07@example.com" and password "ValidPass123456" from client IP "203.0.113.20"
    Then the status code is 429
    And the error follows the standard format with code "RATE_LIMIT_EXCEEDED" and a trace id
    And the response has a Retry-After header greater than 0

  @S08 @NFR-008
  Scenario: Enabled rate limiting applies to registration
    Given an empty PostgreSQL database is running
    And identity-service rate limiting is enabled with permit limit 10 per minute
    And identity-service is configured to use that database and applies migrations on startup
    When I send 11 registration requests with email "rate-s08@example.com" and password "ValidPass123456" from client IP "203.0.113.21"
    Then the status code is 429

  @S09 @NFR-008
  Scenario: Disabled rate limiting does not return 429 for bursts
    Given an empty PostgreSQL database is running
    And identity-service rate limiting is disabled
    And identity-service is configured to use that database and applies migrations on startup
    And a user is registered with email "rate-s09@example.com" and password "ValidPass123456"
    When I send 12 login requests with email "rate-s09@example.com" and password "WrongPass123456" from client IP "203.0.113.22"
    Then no response had status code 429

  @S10 @NFR-007
  Scenario: Lockout records lockout metrics
    Given an empty PostgreSQL database is running
    And identity-service lockout is configured with max failures 5 and lockout duration 60 seconds
    And identity-service metrics are being observed for lockouts
    And identity-service is configured to use that database and applies migrations on startup
    And the email and IP pair for "metric-s10@example.com" is locked from failed logins
    When I send a login request with email "metric-s10@example.com" and password "ValidPass123456"
    Then the status code is 401
    And the identity counter "lockout_total" increased by at least 1