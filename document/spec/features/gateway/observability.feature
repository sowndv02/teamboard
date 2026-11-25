@TB-027 @gateway
Feature: Gateway observability and operations
  As an operator of the API gateway
  I want structured access logs and metrics without leaking secrets
  So that I can run the gateway safely in development and prepare for production observability

  @S01 @NFR-006
  Scenario: A completed API request is access-logged with correlation id and client IP
    Given a fake upstream is running
    And the gateway is configured to point at the fake upstream
    When client "203.0.113.10" sends "GET" to "/api/v1/boards" with X-Correlation-Id "obs-s01-correlation"
    Then the gateway wrote an access log entry with correlationId "obs-s01-correlation" and client IP "203.0.113.10"
    And that access log entry includes method "GET" and status 200
    And that access log entry includes a non-empty trace id

  @S02 @NFR-006
  Scenario: Access logs never contain Authorization values or request bodies
    Given a fake upstream is running
    And the gateway is configured to point at the fake upstream
    When client "203.0.113.11" sends "POST" to "/api/v1/boards" with Authorization "Bearer secret-token-obs-s02" and body "{\"note\":\"private-body-obs-s02\"}"
    Then the gateway wrote an access log entry with correlationId from the response and client IP "203.0.113.11"
    And no gateway log entry contains "secret-token-obs-s02", "private-body-obs-s02" or the request body

  @S03 @NFR-001
  Scenario: Request duration and gateway overhead histograms record after a proxied request
    Given a fake upstream is running
    And the gateway is configured to point at the fake upstream
    When client "203.0.113.12" sends "GET" to "/api/v1/boards"
    Then the gateway histogram "gateway_request_duration_seconds" recorded at least one sample
    And the gateway histogram "gateway_overhead_duration_seconds" recorded at least one sample

  @S04 @NFR-001
  Scenario: The rate limit counter increases when the gateway returns 429
    Given a fake upstream is running
    And the write limit is 1 requests per window
    And the gateway is configured to point at the fake upstream
    And client "203.0.113.13" has used up its write allowance
    When client "203.0.113.13" sends "POST" to "/api/v1/boards"
    Then the gateway answers with status 429 and problem+json code "RATE_LIMIT_EXCEEDED" for "POST"
    And the gateway counter "gateway_http_429" is at least 1

  @S04 @NFR-001
  Scenario: The upstream unavailable counter increases when the gateway returns 503
    Given a fake upstream is running
    And the gateway is configured to point at the fake upstream
    And the fake upstream has stopped
    When client "203.0.113.14" sends "GET" to "/api/v1/boards"
    Then the gateway answers with status 503 and problem+json code "UPSTREAM_UNAVAILABLE" for "GET"
    And the gateway counter "gateway_http_503" is at least 1

  @S05 @NFR-006
  Scenario: The gateway service in compose limits docker log retention
    When I inspect the shipped gateway compose settings
    Then the gateway service uses the json-file log driver with size limits

  @S06 @NFR-006
  Scenario: Graceful shutdown timeouts match operations design
    When I inspect the shipped gateway host and compose settings
    Then the gateway host shutdown timeout is at least 30 seconds
    And the gateway compose service allows 35 seconds to stop

  @S07
  Scenario: Operations documentation warns against public exposure before JWT
    When I inspect the gateway operations runbook
    Then the runbook warns not to expose the gateway on the internet before JWT validation is in place