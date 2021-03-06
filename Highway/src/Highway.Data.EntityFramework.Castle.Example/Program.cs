﻿using System;
using Castle.MicroKernel;
using Castle.MicroKernel.Registration;
using Highway.Data.Interfaces;

namespace Highway.Data.EntityFramework.Castle.Example
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            const string SqlExpressConnectionString =
                @"Data Source=(local)\SQLExpress;Initial Catalog=HighwayDemo;Integrated Security=True";
            var container = new DefaultKernel();
            container.Register(Component.For<IRepository>().ImplementedBy<Repository>(),
                               Component.For<IDataContext>().ImplementedBy<DataContext>().DependsOn(
                                   new {connectionString = SqlExpressConnectionString}),
                               Component.For<IMappingConfiguration>().ImplementedBy<HighwayDataMappings>());

            // Use for Demos
            // DropCreateDatabaseIfModelChanges, Migrations not supported yet.  (IDatabaseInitializers)
            //Database.SetInitializer(new DropCreateDatabaseAlways<EntityFrameworkContext>());

            var application = container.Resolve<DemoApplication>();
            application.Run();

            Console.Read();
        }
    }
}