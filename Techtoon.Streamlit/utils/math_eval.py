"""
Safe arithmetic expression evaluator for numeric input cells.
Supports: + - * / ** % ( ) and numbers.
Usage in Streamlit cells:
    from utils.math_eval import safe_eval
    val = safe_eval(user_input, default=0.0)
"""
import ast
import operator as op

# Supported binary operators
_BIN_OPS = {
    ast.Add: op.add,
    ast.Sub: op.sub,
    ast.Mult: op.mul,
    ast.Div: op.truediv,
    ast.FloorDiv: op.floordiv,
    ast.Mod: op.mod,
    ast.Pow: op.pow,
}

# Supported unary operators
_UNARY_OPS = {
    ast.UAdd: op.pos,
    ast.USub: op.neg,
}


def safe_eval(expression, default=0.0):
    """
    Safely evaluate an arithmetic expression string.
    Returns a float. On any error or empty input, returns `default`.
    """
    if expression is None:
        return default
    s = str(expression).strip()
    if s == "":
        return default
    # Quick sanity: only allow safe characters
    allowed = set("0123456789.+-*/%() \t")
    if not set(s) <= allowed:
        # contains letters or other symbols -> can't evaluate
        return default
    try:
        tree = ast.parse(s, mode="eval")
        return float(_eval_node(tree.body))
    except Exception:
        return default


def _eval_node(node):
    if isinstance(node, ast.Constant):  # Python 3.8+
        if isinstance(node.value, (int, float)):
            return node.value
        raise ValueError("Invalid constant")
    if isinstance(node, ast.Num):  # legacy
        return node.n
    if isinstance(node, ast.BinOp):
        fn = _BIN_OPS.get(type(node.op))
        if fn is None:
            raise ValueError("Unsupported operator")
        return fn(_eval_node(node.left), _eval_node(node.right))
    if isinstance(node, ast.UnaryOp):
        fn = _UNARY_OPS.get(type(node.op))
        if fn is None:
            raise ValueError("Unsupported unary operator")
        return fn(_eval_node(node.operand))
    raise ValueError("Unsupported expression")


def try_eval_or_none(expression):
    """Return evaluated float or None if it cannot be evaluated."""
    if expression is None:
        return None
    s = str(expression).strip()
    if s == "":
        return None
    allowed = set("0123456789.+-*/%() \t")
    if not set(s) <= allowed:
        return None
    try:
        tree = ast.parse(s, mode="eval")
        return float(_eval_node(tree.body))
    except Exception:
        return None
