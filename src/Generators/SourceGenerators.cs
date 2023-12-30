using Microsoft.CodeAnalysis;

namespace Generators
{
    [Generator]
    public class ConfigParameterGenerator : ISourceGenerator
    {
        public void Execute(GeneratorExecutionContext context)
        {
            var contextReceiver = context.SyntaxContextReceiver;

            if (contextReceiver is not FieldSyntaxReciever fieldSyntax)
            {
                throw new NullReferenceException(nameof(fieldSyntax));
            }

            string properties = string.Empty;
            string properties2 = string.Empty;

            foreach (var field in fieldSyntax.IdentifiedFields)
            {
                var propertyName = field.Name.Substring(1);
                propertyName = char.ToUpper(propertyName[0]) + propertyName.Substring(1);

                var property = $$"""

                            [JsonInclude]
                            public {{field.Type}} {{propertyName}}
                            {
                                get => {{field.Name}};
                                set
                                {
                                    if ({{field.Name}} != value)
                                    {
                                        {{field.Name}} = value;
                                        NotifyConfigChanged?.Invoke();
                                        NotifyParameterChanged?.Invoke(nameof({{propertyName}}));
                                    }
                                }
                            }
                    """;

                properties += property;
            }

            var typeName = fieldSyntax.IdentifiedFields.First().ContainingType.ToString().Replace(fieldSyntax.IdentifiedFields.First().ContainingNamespace.ToString(), string.Empty).Substring(1);

            var code = $$"""

                using System.Text.Json.Serialization;

                namespace {{fieldSyntax.IdentifiedFields.First().ContainingNamespace}}
                {
                    public sealed partial class {{typeName}}
                    {
                        {{properties}}
                    }
                }
                """;

            context.AddSource($"{nameof(ConfigParameterGenerator)}_generated.cs", code);
        }

        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => new FieldSyntaxReciever());
        }
    }
}