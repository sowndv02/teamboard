@TB-025 @gateway
Feature: Gateway rejects request bodies over the size limit
  As an operator
  I want oversized request bodies stopped at the gateway
  So that huge requests never reach the services behind it

  # The shipped limit is 1 MiB = 1,048,576 bytes (FR-007). Scenarios that need a small limit say so.
  # Chunked bodies need a real server socket: the in-process test server has no request size limit at all.
  @S01 @FR-007
  Scenario: A body of exactly the limit is forwarded unchanged
    Given a fake upstream is running
    And the gateway is configured to point at the fake upstream
    When I send "POST" to "/api/v1/boards" with a body of 1048576 bytes
    Then the status code is 201
    And the fake upstream received a request body of 1048576 bytes

  @S02 @FR-007
  Scenario: A body one byte over the limit is rejected before it reaches the upstream
    Given a fake upstream is running
    And the gateway is configured to point at the fake upstream
    When I send "POST" to "/api/v1/boards" with a body of 1048577 bytes
    Then the gateway answers with status 413 and problem+json code "PAYLOAD_TOO_LARGE" for "POST"
    And the response has exactly one X-Correlation-Id header that is a GUID
    And the response reveals no host, port or internal name of the upstream
    And the fake upstream receives no request

  @S03 @FR-007 @NFR-004
  Scenario Outline: The limit comes from configuration
    Given a fake upstream is running
    And the body limit is 1024 bytes
    And the gateway is configured to point at the fake upstream
    When I send "POST" to "/api/v1/boards" with a body of <size> bytes
    Then the status code is <status>
    And the number of requests the fake upstream received is <requests>

    Examples:
      | size | status | requests |
      | 1024 | 201    | 1        |
      | 1025 | 413    | 0        |

  @S04 @FR-007
  Scenario: A chunked body of exactly the limit is forwarded unchanged
    Given a fake upstream is running
    And the gateway runs on a real server socket
    And the gateway is configured to point at the fake upstream
    When I send "POST" to "/api/v1/boards" with a chunked body of 1048576 bytes
    Then the status code is 201
    And the fake upstream received a request body of 1048576 bytes

  @S05 @FR-007
  Scenario: A chunked body over the limit is rejected with the same error
    Given a fake upstream is running
    And the gateway runs on a real server socket
    And the gateway is configured to point at the fake upstream
    When I send "POST" to "/api/v1/boards" with a chunked body of 1048577 bytes
    Then the gateway answers with status 413 and problem+json code "PAYLOAD_TOO_LARGE" for "POST"
    And the response has exactly one X-Correlation-Id header that is a GUID

  @S06 @NFR-006
  Scenario: The content of a rejected body is never logged
    Given a fake upstream is running
    And the body limit is 1024 bytes
    And the gateway is configured to point at the fake upstream
    When I send "POST" to "/api/v1/boards" with a body of 2048 bytes made of "secret-board-title-"
    Then the gateway answers with status 413 and problem+json code "PAYLOAD_TOO_LARGE" for "POST"
    And no log line contains "secret-board-title-"

  @S08 @FR-007
  Scenario Outline: The limit applies to every request, not only to the proxied routes
    Given a fake upstream is running
    And the gateway is configured to point at the fake upstream
    When I send "<method>" to "<path>" with a body of 1048577 bytes
    Then the gateway answers with status 413 and problem+json code "PAYLOAD_TOO_LARGE" for "<method>"
    And the fake upstream receives no request

    Examples:
      | method | path           |
      | POST   | /no-such-route |
      | GET    | /health/live   |

  @S09 @FR-007 @NFR-006
  Scenario: The content of a rejected chunked body is never logged
    Given a fake upstream is running
    And the body limit is 1024 bytes
    And the gateway runs on a real server socket
    And the gateway is configured to point at the fake upstream
    When I send "POST" to "/api/v1/boards" with a chunked body of 2048 bytes made of "secret-board-title-"
    Then the gateway answers with status 413 and problem+json code "PAYLOAD_TOO_LARGE" for "POST"
    And no log line contains "secret-board-title-"

  @S07 @FR-004 @FR-007
  Scenario: A client over its request allowance gets 429 before the size check
    Given a fake upstream is running
    And the write limit is 1 requests per window
    And the gateway is configured to point at the fake upstream
    And client "203.0.113.9" has made 1 "POST" request to "/api/v1/boards"
    When client "203.0.113.9" sends "POST" to "/api/v1/boards" with a body of 1048577 bytes
    Then the gateway answers with status 429 and problem+json code "RATE_LIMIT_EXCEEDED" for "POST"
