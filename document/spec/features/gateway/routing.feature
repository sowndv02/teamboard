@TB-010 @gateway
Feature: Gateway forwards the boards API to board-service
  As a client
  I want to call /api/v1/boards through the gateway
  So that I do not need to know the address of board-service

  @S01 @FR-001 @BR-001 @NFR-004
  Scenario Outline: A request to the boards API is forwarded to the upstream with the same method and path
    Given a fake upstream is running
    And the gateway is configured to point at the fake upstream
    When I send "<method>" to "<path>"
    Then the fake upstream receives exactly method "<method>" and path "<path>"
    And the upstream response is returned unchanged to the client

    Examples:
      | method | path |
      | GET    | /api/v1/boards                         |
      | GET    | /api/v1/boards/3f2b1c4e                |
      | GET    | /api/v1/boards?page=2&pageSize=20      |
      | HEAD   | /api/v1/boards                         |
      | POST   | /api/v1/boards                         |
      | PUT    | /api/v1/boards/3f2b1c4e                |
      | PATCH  | /api/v1/boards/3f2b1c4e                |
      | DELETE | /api/v1/boards/3f2b1c4e                |

  @S02 @FR-001
  Scenario Outline: A request matching no route gets 404 problem+json and never reaches the upstream
    Given a fake upstream is running
    And the gateway is configured to point at the fake upstream
    When I send "<method>" to "<path>"
    Then the response is 404 problem+json with code "ROUTE_NOT_FOUND"
    And the response has type and title matching the gateway error code
    And the response has traceId and correlationId
    And the response contains no internal address or name of the upstream
    And the fake upstream receives no request

    Examples:
      | method | path |
      | GET    | /api/v1/unknown     |
      | GET    | /api/v1/boardsX     |
      | GET    | /foo.txt            |
      | TRACE  | /api/v1/boards      |

  @S06 @FR-001
  Scenario: An error returned by the upstream is passed through, not replaced by a gateway error
    Given a fake upstream is running and returns 404 problem+json with code "BOARD_NOT_FOUND" for path "/api/v1/boards/3f2b1c4e"
    And the gateway is configured to point at the fake upstream
    When I send "GET" to "/api/v1/boards/3f2b1c4e"
    Then the response is 404 problem+json with code "BOARD_NOT_FOUND"
    And the response keeps the content returned by the upstream
