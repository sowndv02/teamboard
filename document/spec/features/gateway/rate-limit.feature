@TB-024 @gateway
Feature: Gateway limits requests per client address
  As an operator
  I want each client address limited per route group
  So that spam and overload are stopped before they reach the services behind the gateway

  # Limits are small in most scenarios so a burst fits inside one window; the shipped defaults are checked in S08.
  # Client addresses are chosen per request by the test (documentation range 203.0.113.0/24, 198.51.100.0/24, 2001:db8::/32).
  @S01 @FR-004
  Scenario: A write over the limit is answered with 429 and Retry-After
    Given a fake upstream is running
    And the write limit is 3 requests per window
    And a middleware in front of the gateway adds the header "X-Outer-Middleware" with value "kept"
    And the gateway is configured to point at the fake upstream
    And client "203.0.113.9" has made 3 "POST" requests to "/api/v1/boards"
    When client "203.0.113.9" sends "POST" to "/api/v1/boards"
    Then the gateway answers with status 429 and problem+json code "RATE_LIMIT_EXCEEDED" for "POST"
    And the response has a Retry-After header of 60 seconds
    And the response has exactly one X-Correlation-Id header that is a GUID
    And the response keeps the header "X-Outer-Middleware" with value "kept"
    And the response does not reveal the client address "203.0.113.9"

  @S02 @FR-004
  Scenario Outline: Reads over the limit are answered with 429 and GET and HEAD share one allowance
    Given a fake upstream is running
    And the read limit is 3 requests per window
    And the gateway is configured to point at the fake upstream
    And client "203.0.113.9" has made 2 "GET" requests to "/api/v1/boards"
    And client "203.0.113.9" has made 1 "HEAD" request to "/api/v1/boards"
    When client "203.0.113.9" sends "<method>" to "/api/v1/boards"
    Then the gateway answers with status 429 and problem+json code "RATE_LIMIT_EXCEEDED" for "<method>"
    And the response has a Retry-After header of 60 seconds

    Examples:
      | method |
      | GET    |
      | HEAD   |

  @S03 @FR-004
  Scenario Outline: Other clients, the other route group, health and unknown paths are not affected
    Given a fake upstream is running
    And the write limit is 3 requests per window
    And the read limit is 3 requests per window
    And the gateway is configured to point at the fake upstream
    And client "203.0.113.9" has used up its <group> allowance
    When client "<client>" sends "<method>" to "<path>"
    Then the status code is <status>

    Examples:
      | group | client       | method | path           | status |
      | write | 203.0.113.10 | POST   | /api/v1/boards | 201    |
      | write | 203.0.113.9  | GET    | /api/v1/boards | 200    |
      | write | 203.0.113.9  | GET    | /health/live   | 200    |
      | write | 203.0.113.9  | GET    | /health/ready  | 200    |
      | write | 203.0.113.9  | GET    | /no-such-route | 404    |
      | read  | 203.0.113.10 | GET    | /api/v1/boards | 200    |
      | read  | 203.0.113.9  | POST   | /api/v1/boards | 201    |
      | read  | 203.0.113.9  | GET    | /health/live   | 200    |
      | read  | 203.0.113.9  | GET    | /health/ready  | 200    |
      | read  | 203.0.113.9  | GET    | /no-such-route | 404    |

  @S04 @FR-004
  Scenario Outline: IPv6 addresses in the same /64 share one allowance
    Given a fake upstream is running
    And the write limit is 3 requests per window
    And the gateway is configured to point at the fake upstream
    And client "2001:db8:abcd:1::1" has made 3 "POST" requests to "/api/v1/boards"
    When client "<client>" sends "POST" to "/api/v1/boards"
    Then the status code is <status>

    Examples:
      | client                  | status |
      | 2001:db8:abcd:1:ffff::2 | 429    |
      | 2001:db8:abcd:2::1      | 201    |

  @S05 @FR-004
  Scenario: A client that is not a trusted proxy cannot escape the limit with X-Forwarded-For
    Given a fake upstream is running
    And the write limit is 3 requests per window
    And the gateway is configured to point at the fake upstream
    And client "198.51.100.77" has made 3 "POST" requests to "/api/v1/boards" with a different X-Forwarded-For each time
    When client "198.51.100.77" sends "POST" to "/api/v1/boards" with a new X-Forwarded-For
    Then the gateway answers with status 429 and problem+json code "RATE_LIMIT_EXCEEDED" for "POST"

  @S06 @FR-004
  Scenario Outline: A trusted proxy passes the real client address in X-Forwarded-For
    Given a fake upstream is running
    And the write limit is 3 requests per window
    And the gateway trusts the proxy "10.0.0.5"
    And the gateway is configured to point at the fake upstream
    And the proxy "10.0.0.5" has made 3 "POST" requests to "/api/v1/boards" on behalf of "203.0.113.7"
    When the proxy "10.0.0.5" sends "POST" to "/api/v1/boards" on behalf of "<client>"
    Then the status code is <status>

    Examples:
      | client       | status |
      | 203.0.113.7  | 429    |
      | 203.0.113.8  | 201    |

  @S07 @FR-004
  Scenario: The upstream receives the forwarded headers the gateway determined itself
    Given a fake upstream is running
    And the gateway is configured to point at the fake upstream
    When client "198.51.100.4" sends "GET" to "/api/v1/boards" with X-Forwarded-For "6.6.6.6" and X-Forwarded-Proto "evil"
    Then the fake upstream received X-Forwarded-For "198.51.100.4" and X-Forwarded-Proto "http"

  @S08 @FR-004 @NFR-004
  Scenario Outline: The shipped limits are 30 writes and 120 reads per minute
    Given a fake upstream is running
    And the gateway is configured to point at the fake upstream
    And client "203.0.113.9" has used up its <group> allowance
    When client "203.0.113.9" sends "<method>" to "/api/v1/boards"
    Then the gateway answers with status 429 and problem+json code "RATE_LIMIT_EXCEEDED" for "<method>"

    Examples:
      | group | method |
      | write | POST   |
      | read  | GET    |

  @S11 @FR-004
  Scenario: With a trusted proxy list configured, a peer outside the list still cannot escape the limit
    Given a fake upstream is running
    And the write limit is 3 requests per window
    And the gateway trusts the proxy "10.0.0.5"
    And the gateway is configured to point at the fake upstream
    And client "198.51.100.77" has made 3 "POST" requests to "/api/v1/boards" with a different X-Forwarded-For each time
    When client "198.51.100.77" sends "POST" to "/api/v1/boards" with a new X-Forwarded-For
    Then the gateway answers with status 429 and problem+json code "RATE_LIMIT_EXCEEDED" for "POST"

  @S12 @FR-004
  Scenario Outline: A proxy inside a trusted network passes the real client address
    Given a fake upstream is running
    And the write limit is 3 requests per window
    And the gateway trusts the network "10.0.0.0/8"
    And the gateway is configured to point at the fake upstream
    And the proxy "10.1.2.3" has made 3 "POST" requests to "/api/v1/boards" on behalf of "203.0.113.7"
    When the proxy "10.1.2.3" sends "POST" to "/api/v1/boards" on behalf of "<client>"
    Then the status code is <status>

    Examples:
      | client       | status |
      | 203.0.113.7  | 429    |
      | 203.0.113.8  | 201    |

  @S13 @FR-004
  Scenario: Retry-After follows the configured window
    Given a fake upstream is running
    And the write limit is 3 requests per window
    And the write window is 120 seconds
    And the gateway is configured to point at the fake upstream
    And client "203.0.113.9" has made 3 "POST" requests to "/api/v1/boards"
    When client "203.0.113.9" sends "POST" to "/api/v1/boards"
    Then the gateway answers with status 429 and problem+json code "RATE_LIMIT_EXCEEDED" for "POST"
    And the response has a Retry-After header of 120 seconds

  @S14 @FR-004
  Scenario: Requests without a known client address share one allowance
    Given a fake upstream is running
    And the write limit is 3 requests per window
    And the gateway is configured to point at the fake upstream
    And an unidentified client has made 3 "POST" requests to "/api/v1/boards"
    When an unidentified client sends "POST" to "/api/v1/boards"
    Then the gateway answers with status 429 and problem+json code "RATE_LIMIT_EXCEEDED" for "POST"
