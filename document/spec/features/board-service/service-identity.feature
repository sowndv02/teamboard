@TB-001 @board-service
Feature: Consistent board-service name
  As a developer
  I want the service to have a single name everywhere
  So that later issues (gateway, task) reference the right name

  @S01 @FR-001
  Scenario: The service assembly name and root namespace are board-service
    When I inspect the service assembly
    Then the assembly name is "board-service"
    And the root namespace of the service project is "BoardService"

  @S02 @FR-001
  Scenario: No current file still contains the old service name
    When I scan the current source code, configuration and documentation
    Then no file contains the old service name

  @S03 @FR-001 @docker
  Scenario: Compose runs the board-service service on host port 6060
    Given Docker is available
    When I run the main compose under a dedicated project name
    Then the list of running services includes "board-service"
    And host port 6060 responds with an HTTP status code
