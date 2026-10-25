@TB-023 @gateway
Feature: Gateway reports downstream failures clearly
  As a client of the API
  I want a failure behind the gateway to be reported as a clear, uniform error
  So that I can tell a gateway or service problem apart and retry sensibly

  # Retry-After is only sent with 503 (api-endpoints.md section 6).
  @S01 @FR-006
  Scenario Outline: An unreachable upstream is reported as 503 with Retry-After
    Given a fake upstream is running
    And the gateway is configured to point at the fake upstream
    And the fake upstream has stopped
    When I send "<method>" to "/api/v1/boards"
    Then the gateway answers with status 503 and problem+json code "UPSTREAM_UNAVAILABLE" for "<method>"
    And the response has a Retry-After header of 5 seconds
    And the response has exactly one X-Correlation-Id header that is a GUID
    And the response reveals no host, port or internal name of the upstream
    And the gateway logged a warning in the correlation scope of the response

    Examples:
      | method |
      | GET    |
      | HEAD   |
      | POST   |

  @S02 @FR-006
  Scenario: No upstream instance available is reported as 503
    Given the gateway runs with no upstream instance available
    When I send "GET" to "/api/v1/boards"
    Then the gateway answers with status 503 and problem+json code "UPSTREAM_UNAVAILABLE" for "GET"
    And the response has a Retry-After header of 5 seconds
    And the response reveals no host, port or internal name of the upstream

  @S03 @FR-006
  Scenario: An upstream that closes the connection before sending headers is reported as 502
    Given a fake upstream is running that closes the connection before sending headers
    And the gateway is configured to point at the fake upstream
    When I send "GET" to "/api/v1/boards"
    Then the gateway answers with status 502 and problem+json code "UPSTREAM_BAD_GATEWAY" for "GET"
    And the response has no Retry-After header
    And the response has exactly one X-Correlation-Id header that is a GUID
    And the response reveals no host, port or internal name of the upstream
    And the gateway logged a warning in the correlation scope of the response

  @S04 @FR-006 @NFR-003
  Scenario: An upstream that outlasts the time allowed for the whole request is reported as 504
    Given a fake upstream is running that hangs without sending headers
    And the time allowed for the whole request is "00:00:01"
    And the time allowed for upstream silence is "00:00:30"
    And the gateway is configured to point at the fake upstream
    When I send "GET" to "/api/v1/boards" with X-Correlation-Id "client-id-504"
    Then the gateway answers with status 504 and problem+json code "UPSTREAM_TIMEOUT" for "GET"
    And the response has no Retry-After header
    And the response has exactly one X-Correlation-Id header equal to "client-id-504"
    And the response reveals no host, port or internal name of the upstream
    And the gateway logged a warning in the correlation scope of the response

  @S05 @FR-006 @NFR-003
  Scenario: An upstream that stays silent too long is reported as 504
    Given a fake upstream is running that hangs without sending headers
    And the time allowed for the whole request is "00:00:30"
    And the time allowed for upstream silence is "00:00:01"
    And the gateway is configured to point at the fake upstream
    When I send "GET" to "/api/v1/boards"
    Then the gateway answers with status 504 and problem+json code "UPSTREAM_TIMEOUT" for "GET"
    And the response has no Retry-After header
    And the response reveals no host, port or internal name of the upstream

  @S06 @FR-006
  Scenario: An upstream that closes the connection after sending headers is cut and counted
    Given a fake upstream is running that sends headers and then closes the connection
    And the gateway is configured to point at the fake upstream
    When I send "GET" to "/api/v1/boards" and read the whole response body
    Then the client gets an error while reading the response body
    And the gateway logged a warning in the correlation scope of the response
    And the aborted-upstream counter is exactly 1
    And the logged gateway warning reveals no host, port or internal name of the upstream

  @S07 @FR-006 @NFR-003
  Scenario Outline: An upstream that hangs after sending headers is cut and counted
    Given a fake upstream is running that sends headers and then hangs
    And the time allowed for the whole request is "<whole request>"
    And the time allowed for upstream silence is "<silence>"
    And the gateway is configured to point at the fake upstream
    When I send "GET" to "/api/v1/boards" and read the whole response body
    Then the client gets an error while reading the response body
    And the gateway logged a warning in the correlation scope of the response
    And the aborted-upstream counter is exactly 1

    Examples:
      | whole request | silence  |
      | 00:00:02      | 00:00:30 |
      | 00:00:30      | 00:00:02 |

  @S09 @FR-002
  Scenario Outline: Health stays up while the upstream is down
    Given a fake upstream is running
    And the gateway is configured to point at the fake upstream
    And the fake upstream has stopped
    When I send "GET" to "<path>"
    Then the status code is 200

    Examples:
      | path          |
      | /health/live  |
      | /health/ready |

  @S10 @FR-006 @docker
  Scenario: In compose, a gateway whose upstream port is closed answers 503
    Given Docker is available
    And the gateway runs alone in compose with its upstream address pointing at a closed port
    When I call "GET /api/v1/boards" on host port 8080
    Then the gateway answered on host port 8080 with status 503 and problem+json code "UPSTREAM_UNAVAILABLE"

  @S11 @FR-006
  Scenario: Retry-After is read from configuration
    Given a fake upstream is running
    And the Retry-After setting is "7"
    And the gateway is configured to point at the fake upstream
    And the fake upstream has stopped
    When I send "GET" to "/api/v1/boards"
    Then the gateway answers with status 503 and problem+json code "UPSTREAM_UNAVAILABLE" for "GET"
    And the response has a Retry-After header of 7 seconds

  @S12 @NFR-006
  Scenario: Credentials, query values and body never reach the gateway's own logs
    Given a fake upstream is running
    And the gateway is configured to point at the fake upstream
    And the fake upstream has stopped
    When I send "POST" to "/api/v1/boards?token=secret-query-value" with Authorization "Bearer secret-bearer-value" and body "secret-body-value"
    Then the gateway answers with status 503 and problem+json code "UPSTREAM_UNAVAILABLE" for "POST"
    And no log written by the gateway itself contains "secret-query-value", "secret-bearer-value" or "secret-body-value"
