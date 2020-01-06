﻿using System;
using System.IO;
using System.Text;
using jsonschema.FluentValidation;
using NJsonSchema;
using NJsonSchema.CodeGeneration.TypeScript;
using NJsonSchema.Generation;
using WebTty.Messages.Helpers;

namespace jsonschema
{
    class Program
    {
        static void Main(string[] args)
        {
            var root = "src/WebTty.UI/Client/.tmp/messages";

            var validatorFactory = new ValidatorFactory(typeof(IMessageResolver).Assembly);
            var schemaProcessor = new FluentValidationSchemaProcessor(validatorFactory);

            var settings = new JsonSchemaGeneratorSettings();
            settings.SchemaProcessors.Add(schemaProcessor);
            settings.DefaultReferenceTypeNullHandling = ReferenceTypeNullHandling.NotNull;

            var schema = new JsonSchema(); // the schema to write into
            var resolver = new JsonSchemaResolver(schema, settings); // used to add and retrieve schemas from the 'definitions'
            var generator = new JsonSchemaGenerator(settings);
            var tsSettings = new TypeScriptGeneratorSettings
            {
                TypeStyle = TypeScriptTypeStyle.Class,
                TypeScriptVersion = 3.5m,
            };

            var mainModule = "";
            var messages = new MessageResolver();

            foreach (var type in messages.GetMessages())
            {
                var schemaForType = generator.Generate(type, resolver);
                schemaForType.Title = type.Name;

                var codeGenerator = new TypeScriptGenerator(schemaForType, tsSettings);
                var code = codeGenerator.GenerateFile();

                Directory.CreateDirectory(root);

                using (var sourceFile = File.Open($"{root}/{type.Name}.ts", FileMode.Create))
                {
                    sourceFile.Write(Encoding.UTF8.GetBytes(code));
                    sourceFile.Flush();
                }

                mainModule += $"export * from './{type.Name}'\n";
            }

            using (var mainFile = File.Open($"{root}/index.ts", FileMode.Create))
            {
                mainFile.Write(Encoding.UTF8.GetBytes(mainModule));
                mainFile.Flush();
            }

            Console.WriteLine(schema.ToJson());
        }
    }
}
