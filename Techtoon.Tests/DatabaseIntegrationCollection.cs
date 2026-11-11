using Xunit;

namespace Techtoon.Tests;

/// <summary>
/// مجموعة اختبارات قاعدة البيانات — تُنفَّذ بالتسلسل (وليس بالتوازي).
/// السبب: الاختبارات تشارك نفس الخزينة والحسابات في القاعدة المعزولة،
/// فالتوازي يُفسد قياسات رصيد الخزينة.
/// </summary>
[CollectionDefinition("DatabaseIntegration", DisableParallelization = true)]
public class DatabaseIntegrationCollection : ICollectionFixture<DatabaseTestCleanupFixture>
{
}
