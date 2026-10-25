@TB-022 @gateway
Feature: Trace spans gateway and board-service
  As an operator
  I want to see a request passing through the gateway and a service in a single trace
  So that I can locate slowness or errors in the right service

  @S06 @NFR-002 @NFR-004 @docker
  Scenario: One trace in Jaeger has spans of both gateway and board-service
    Given Docker is available
    And gateway, board-service and jaeger are running with compose under a dedicated project name
    When I call "GET /api/v1/boards" through host port 8080 with a test-generated traceparent
    Then Jaeger has a trace for that trace id containing spans of services "gateway" and "board-service"
