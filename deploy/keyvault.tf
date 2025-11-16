data "azurerm_client_config" "current" {}

resource "azurerm_key_vault" "kv-presettie" {
  resource_group_name = azurerm_resource_group.rg-presettie.name
  location            = azurerm_resource_group.rg-presettie.location

  name                       = "kv-presettie-${var.env}"
  tenant_id                  = data.azurerm_client_config.current.tenant_id
  sku_name                   = "standard"
  purge_protection_enabled   = false
  soft_delete_retention_days = 7

  tags = local.tags
}

resource "azurerm_key_vault_access_policy" "kvap-terraform" {
  key_vault_id = azurerm_key_vault.kv-presettie.id
  tenant_id    = data.azurerm_client_config.current.tenant_id
  object_id    = data.azurerm_client_config.current.object_id

  secret_permissions = [
    "Get",
    "List",
    "Set",
    "Delete",
    "Purge",
    "Recover"
  ]
}

resource "azurerm_key_vault_secret" "kvs-telegram-token" {
  key_vault_id = azurerm_key_vault.kv-presettie.id
  name         = "Telegram--Token"
  value        = var.telegram-token

  depends_on = [azurerm_key_vault_access_policy.kvap-terraform]
}

resource "azurerm_key_vault_secret" "kvs-telegram-bot-url" {
  key_vault_id = azurerm_key_vault.kv-presettie.id
  name         = "Telegram--BotUrl"
  value        = var.telegram-bot-url

  depends_on = [azurerm_key_vault_access_policy.kvap-terraform]
}

resource "azurerm_key_vault_secret" "kvs-auth-client-id" {
  key_vault_id = azurerm_key_vault.kv-presettie.id
  name         = "Auth--ClientId"
  value        = var.auth-client-id

  depends_on = [azurerm_key_vault_access_policy.kvap-terraform]
}

resource "azurerm_key_vault_secret" "kvs-auth-client-secret" {
  key_vault_id = azurerm_key_vault.kv-presettie.id
  name         = "Auth--ClientSecret"
  value        = var.auth-client-secret

  depends_on = [azurerm_key_vault_access_policy.kvap-terraform]
}

resource "azurerm_key_vault_secret" "kvs-auth-callback-url" {
  key_vault_id = azurerm_key_vault.kv-presettie.id
  name         = "Auth--CallbackUrl"
  value        = var.auth-callback-url

  depends_on = [azurerm_key_vault_access_policy.kvap-terraform]
}

resource "azurerm_key_vault_secret" "kvs-authentication-authority" {
  key_vault_id = azurerm_key_vault.kv-presettie.id
  name         = "Authentication--Schemes--Bearer--Authority"
  value        = var.jwt-authority

  depends_on = [azurerm_key_vault_access_policy.kvap-terraform]
}

resource "azurerm_key_vault_secret" "kvs-authentication-audience" {
  key_vault_id = azurerm_key_vault.kv-presettie.id
  name         = "Authentication--Schemes--Bearer--ValidAudience"
  value        = var.jwt-audience

  depends_on = [azurerm_key_vault_access_policy.kvap-terraform]
}

resource "azurerm_key_vault_secret" "kvs-authentication-issuer" {
  key_vault_id = azurerm_key_vault.kv-presettie.id
  name         = "Authentication--Schemes--Bearer--ValidIssuer"
  value        = var.jwt-issuer

  depends_on = [azurerm_key_vault_access_policy.kvap-terraform]
}

resource "azurerm_key_vault_secret" "kvs-database-connection-string" {
  key_vault_id = azurerm_key_vault.kv-presettie.id
  name         = "Database--ConnectionString"
  value        = var.database-connection-string

  depends_on = [azurerm_key_vault_access_policy.kvap-terraform]
}

resource "azurerm_key_vault_secret" "kvs-redis-connection-string" {
  key_vault_id = azurerm_key_vault.kv-presettie.id
  name         = "Redis--ConnectionString"
  value        = var.redis-connection-string

  depends_on = [azurerm_key_vault_access_policy.kvap-terraform]
}

resource "azurerm_key_vault_secret" "kvs-storage-connection-string" {
  key_vault_id = azurerm_key_vault.kv-presettie.id
  name         = "Storage--ConnectionString"
  value        = azurerm_storage_account.st-presettie.primary_connection_string

  depends_on = [azurerm_key_vault_access_policy.kvap-terraform]
}

resource "azurerm_key_vault_access_policy" "kvap-func-bot" {
  key_vault_id = azurerm_key_vault.kv-presettie.id
  tenant_id    = azurerm_linux_function_app.func-presettie-bot.identity[0].tenant_id
  object_id    = azurerm_linux_function_app.func-presettie-bot.identity[0].principal_id

  secret_permissions = [
    "Get",
    "List"
  ]
}

resource "azurerm_key_vault_access_policy" "kvap-func-api" {
  key_vault_id = azurerm_key_vault.kv-presettie.id
  tenant_id    = azurerm_linux_function_app.func-presettie-api.identity[0].tenant_id
  object_id    = azurerm_linux_function_app.func-presettie-api.identity[0].principal_id

  secret_permissions = [
    "Get",
    "List"
  ]
}
