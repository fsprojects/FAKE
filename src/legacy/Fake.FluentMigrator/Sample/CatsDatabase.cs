using FluentMigrator;

namespace Sample
{
    [Migration(1, "Create cats table")]
    public class CreateCatsTable: AutoReversingMigration
    {
        public override void Up()
        {
            Create.Table("Cats")
                .WithColumn("Id").AsInt32().PrimaryKey()
                .WithColumn("Name").AsString(50).NotNullable();
        }
    }

    [Migration(2, "Create dogs table")]
    public class CreateDogsTable : AutoReversingMigration
    {
        public override void Up()
        {
            Create.Table("Dogs")
                .WithColumn("Id").AsInt32().PrimaryKey()
                .WithColumn("Age").AsInt32().NotNullable()
                .WithColumn("Name").AsString(50).NotNullable();
        }
    }

    [Migration(3, "Create foods table")]
    public class CreateFoodsTable : AutoReversingMigration
    {
        public override void Up()
        {
            Create.Table("Foods")
                .WithColumn("Id").AsInt32().PrimaryKey()
                .WithColumn("Title").AsString(100).NotNullable();
        }
    }
}