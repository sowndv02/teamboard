@TB-026 @gateway
Feature: Gateway answers cross-origin browser calls only for allowed origins
  As a web client served from an allowed origin
  I want the gateway to answer CORS preflight and actual requests for that origin
  So that the browser lets the page call the API, while other origins get no permission

  # The default allowlist comes from appsettings.json (http://localhost:4200). Scenarios that need
  # another allowlist set it with the Given step before the gateway starts.
  # Preflight means an OPTIONS request with an Origin header; a simple request is a GET/POST/... with an Origin header.

  @S01 @FR-005
  Scenario: A preflight from an allowed origin is answered by the gateway and never reaches the service
    Given a fake upstream is running
    And the gateway is configured to point at the fake upstream
    When a browser at origin "http://localhost:4200" makes a preflight request for "POST" to "/api/v1/boards"
    Then the status code is 204
    And the response allows origin "http://localhost:4200"
    And the response allows the methods "GET, HEAD, POST, PUT, PATCH, DELETE, OPTIONS"
    And the response allows the request headers "Authorization, Content-Type, X-Correlation-Id, Idempotency-Key, If-Match"
    And the response has a preflight max age of 600 seconds
    And the response has Vary containing "Origin"
    And the fake upstream receives no request

  @S02 @FR-005
  Scenario Outline: An origin outside the allowlist gets no CORS permission
    Given a fake upstream is running
    And the gateway is configured to point at the fake upstream
    When a browser at origin "http://evil.example" makes a <kind> request for "<method>" to "/api/v1/boards"
    Then the response has no Access-Control-Allow-Origin header

    Examples:
      | kind      | method |
      | preflight | POST   |
      | simple    | GET    |

  @S03 @FR-005
  Scenario: A simple request from an allowed origin exposes the correlation and retry headers
    Given a fake upstream is running
    And the gateway is configured to point at the fake upstream
    When a browser at origin "http://localhost:4200" makes a simple request for "GET" to "/api/v1/boards"
    Then the status code is 200
    And the response allows origin "http://localhost:4200"
    And the response exposes the headers "X-Correlation-Id, Retry-After, ETag, Location"
    And the response has Vary containing "Origin"

  @S04 @FR-005
  Scenario: A preflight is not counted against the rate limit
    Given a fake upstream is running
    And the write limit is 3 requests per window
    And the gateway is configured to point at the fake upstream
    And client "203.0.113.9" has used up its write allowance
    When a browser at origin "http://localhost:4200" makes a preflight request for "POST" to "/api/v1/boards" from client "203.0.113.9"
    Then the status code is 204
    And the response allows origin "http://localhost:4200"

  @S05 @FR-005
  Scenario: A 429 answered to an allowed origin still carries the CORS headers
    Given a fake upstream is running
    And the write limit is 3 requests per window
    And the gateway is configured to point at the fake upstream
    And client "203.0.113.9" has made 3 "POST" requests to "/api/v1/boards"
    When a browser at origin "http://localhost:4200" makes a simple request for "POST" to "/api/v1/boards" from client "203.0.113.9"
    Then the gateway answers with status 429 and problem+json code "RATE_LIMIT_EXCEEDED" for "POST"
    And the response has a Retry-After header of 60 seconds
    And the response allows origin "http://localhost:4200"

  @S06 @FR-005
  Scenario: The gateway never allows credentials or every origin
    Given a fake upstream is running
    And the gateway is configured to point at the fake upstream
    When a browser at origin "http://localhost:4200" makes a preflight request for "POST" to "/api/v1/boards"
    Then the response has no Access-Control-Allow-Credentials header
    And the response never allows every origin

  @S07 @FR-005 @NFR-004
  Scenario Outline: The allowed origins come from configuration
    Given a fake upstream is running
    And the allowed origin is "https://board.example.test"
    And the gateway is configured to point at the fake upstream
    When a browser at origin "<origin>" makes a preflight request for "POST" to "/api/v1/boards"
    Then the origin "<origin>" is <result> by CORS

    Examples:
      | origin                     | result  |
      | https://board.example.test | allowed |
      | http://localhost:4200      | blocked |
