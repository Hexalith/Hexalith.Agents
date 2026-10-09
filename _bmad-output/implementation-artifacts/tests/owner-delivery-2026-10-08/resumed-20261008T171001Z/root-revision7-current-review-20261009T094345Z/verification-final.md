### Catalogue opt-in registration is bypassed by the active-count tests

- **Changed surface:** conversations/src/Hexalith.Conversations.Server/Agents/ConversationAgentServiceCollectionExtensions.cs:48 replaces the unavailable catalogue with EventStoreConversationTenantCatalogue.
- **Impacted consumer:** ConversationActiveCountQueryHandler at conversations/src/Hexalith.Conversations.Server/Agents/ConversationActiveCountQueryHandler.cs:32 uses the injected ConversationAgentQueryService to obtain active counts.
- **Existing test evidence:** EventStoreConversationTenantCatalogueTests.cs:48 constructs the catalogue directly; its count assertions at lines 80 and 147 also construct the query service directly. ConversationAgentSixSeamTests.cs:530 manually registers a fixture catalogue. Workspace searches for AddConversationTenantCatalogue, ConversationTenantCatalogueRegistration, and catalogue consumers found no test invoking the new registration API.
- **Missing verification:** An active-count assertion through services composed with AddConversationTenantCatalogue.
- **Demonstration:** Removing the Replace call would leave the unavailable default registered. An opted-in host would return Unavailable, while the tests checked would continue using their directly supplied catalogues.
- **Consequence:** The new registration API could silently fail to enable complete active counts.
- **Disposition:** patch — add a test to EventStoreConversationTenantCatalogueTests that invokes both registration methods, supplies its existing source and authority fixtures, resolves the scoped query service, and asserts the complete active count.

