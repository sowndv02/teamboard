@TB-021 @identity-service @docker
Feature: Run identity-service with compose
  As a developer
  I want to start identity-service together with its database using one compose command
  So that I can run and try the service locally the same way it runs in a real environment

  @S05 @NFR-001
  Scenario: Compose starts identity-service with PostgreSQL and ready passes within 30 seconds
    Given Docker is available
    And the identity-service image is built with the main compose file
    When I run identity-service with PostgreSQL using compose with a dedicated project name
    Then host port 6061 returns 200 for "/health/ready" within 30 seconds of the start command

  @S09 @D02
  Scenario: The identity database user has no admin privileges
    Given Docker is available
    And the identity-service image is built with the main compose file
    When I run identity-service with PostgreSQL using compose with a dedicated project name
    Then the identity database user has no admin privileges

  @S10 @D02
  Scenario: Another database user cannot connect to the identity database
    Given Docker is available
    And the identity-service image is built with the main compose file
    When I run identity-service with PostgreSQL using compose with a dedicated project name
    Then another database user on the same instance cannot connect to the identity database
