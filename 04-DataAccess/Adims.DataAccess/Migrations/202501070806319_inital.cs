namespace Adims.DataAccess.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class inital : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.City",
                c => new
                    {
                        Id = c.Guid(nullable: false, identity: true),
                        Name = c.String(),
                        CreatedDate = c.DateTime(nullable: false),
                        LastModifeDate = c.DateTime(),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.Dealer",
                c => new
                    {
                        Id = c.Guid(nullable: false, identity: true),
                        OwnerName = c.String(),
                        DealerNo = c.String(),
                        InActive = c.Boolean(nullable: false),
                        CityId = c.Guid(nullable: false),
                        CreatedDate = c.DateTime(nullable: false),
                        LastModifeDate = c.DateTime(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.City", t => t.CityId, cascadeDelete: true)
                .Index(t => t.CityId);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Dealer", "CityId", "dbo.City");
            DropIndex("dbo.Dealer", new[] { "CityId" });
            DropTable("dbo.Dealer");
            DropTable("dbo.City");
        }
    }
}
