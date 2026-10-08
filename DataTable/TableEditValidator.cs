using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Mochi.DataTable
{
    /// <summary>
    /// 编辑期校验器:对内存中的行数组执行表内结构检查(键存在、必填、重复键)、
    /// 声明式跨表引用检查与领域规则,一次调用收齐全部错误。
    /// 与装载管线的分工:装载只做表内结构校验;引用合法性与领域规则只在这里执行,
    /// 因此绕过编辑器的手改数据不会被装载拒绝。
    /// </summary>
    public static class TableEditValidator
    {
        /// <summary>
        /// 校验一张表的全部行数据。行键取每行的 <see cref="TableRow.id"/>。
        /// </summary>
        /// <param name="definition">表声明,其引用与规则挂点参与校验。</param>
        /// <param name="rows">行对象数组;通常来自编辑工具对表文件的解析。</param>
        /// <param name="keyResolver">跨表键解析器;传 null 时跳过引用检查(无跨表上下文的场景)。</param>
        /// <exception cref="ArgumentNullException">definition 或 rows 为 null。</exception>
        public static IReadOnlyList<TableEditError> ValidateTable<TRow>(
            TableDefinition<TRow> definition,
            JArray rows,
            IEditTimeKeyResolver keyResolver)
            where TRow : TableRow
        {
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }
            if (rows == null)
            {
                throw new ArgumentNullException(nameof(rows));
            }

            var errors = new List<TableEditError>();
            var seenKeys = new HashSet<string>();
            int ordinal = 0;
            foreach (JToken entry in rows)
            {
                ordinal++;
                if (entry.Type != JTokenType.Object)
                {
                    errors.Add(new TableEditError(null, $"第 {ordinal} 个条目不是 JSON 对象。"));
                    continue;
                }

                var entryObject = (JObject)entry;
                string keyFieldName = TableRow.IdFieldName;
                string entryId = ReadTokenString(entryObject[keyFieldName]);

                if (string.IsNullOrEmpty(entryId))
                {
                    errors.Add(new TableEditError(
                        keyFieldName,
                        $"第 {ordinal} 个条目缺少键字段 {keyFieldName} 或其值为空。"));
                }
                else if (!seenKeys.Add(entryId))
                {
                    errors.Add(new TableEditError(
                        keyFieldName,
                        $"条目 {entryId} 与其他条目的键 {keyFieldName} 重复。"));
                }

                foreach (string field in definition.RequiredFields)
                {
                    if (!IsFieldPresent(entryObject, field))
                    {
                        errors.Add(new TableEditError(
                            field,
                            $"条目 {entryId ?? ("第 " + ordinal + " 个")} 缺少必填字段 {field}。"));
                    }
                }

                ValidateReferences(definition, entryObject, entryId, keyResolver, errors);
                ValidateRules(definition, entryObject, entryId, keyResolver, errors);
            }

            return errors;
        }

        private static void ValidateReferences<TRow>(
            TableDefinition<TRow> definition,
            JObject entry,
            string entryId,
            IEditTimeKeyResolver keyResolver,
            List<TableEditError> errors)
            where TRow : TableRow
        {
            if (keyResolver == null)
            {
                return;
            }

            foreach (TableReference reference in definition.References)
            {
                JToken token = entry[reference.FieldName];
                if (token == null || token.Type == JTokenType.Null)
                {
                    continue;
                }

                if (token.Type == JTokenType.Array)
                {
                    foreach (JToken element in (JArray)token)
                    {
                        CheckSingleReference(reference, element, entryId, keyResolver, errors);
                    }
                }
                else
                {
                    CheckSingleReference(reference, token, entryId, keyResolver, errors);
                }
            }
        }

        private static void CheckSingleReference(
            TableReference reference,
            JToken token,
            string entryId,
            IEditTimeKeyResolver keyResolver,
            List<TableEditError> errors)
        {
            string value = ReadTokenString(token);
            if (string.IsNullOrEmpty(value))
            {
                return;
            }

            if (!keyResolver.ContainsKey(reference.TargetTableName, value))
            {
                var error = new TableEditError(
                    reference.FieldName,
                    $"引用的表 {reference.TargetTableName} 中不存在条目 {value}。");
                error.EntryId = entryId;
                errors.Add(error);
            }
        }

        private static void ValidateRules<TRow>(
            TableDefinition<TRow> definition,
            JObject entry,
            string entryId,
            IEditTimeKeyResolver keyResolver,
            List<TableEditError> errors)
            where TRow : TableRow
        {
            if (definition.EditRules.Count == 0)
            {
                return;
            }

            TRow row;
            try
            {
                row = entry.ToObject<TRow>();
            }
            catch (Exception ex)
            {
                errors.Add(new TableEditError(
                    null,
                    $"条目 {entryId} 无法反序列化为 {typeof(TRow).Name},领域规则未执行: {ex.Message}"));
                return;
            }

            foreach (ITableEditRule<TRow> rule in definition.EditRules)
            {
                IEnumerable<TableEditError> violations;
                try
                {
                    violations = rule.Validate(row, keyResolver);
                }
                catch (Exception ex)
                {
                    errors.Add(new TableEditError(
                        null,
                        $"条目 {entryId} 上的规则 {rule.GetType().Name} 执行异常: {ex.Message}"));
                    continue;
                }

                if (violations == null)
                {
                    continue;
                }
                foreach (TableEditError violation in violations)
                {
                    violation.EntryId = entryId;
                    errors.Add(violation);
                }
            }
        }

        private static string ReadTokenString(JToken token)
        {
            if (token == null || token.Type == JTokenType.Null)
            {
                return null;
            }
            return token.ToString();
        }

        private static bool IsFieldPresent(JObject entry, string fieldName)
        {
            JToken token = entry[fieldName];
            return token != null && token.Type != JTokenType.Null;
        }
    }
}
