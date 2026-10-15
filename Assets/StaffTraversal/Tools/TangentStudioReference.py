# Exported from Manim Studio. Run with the project ManimGL environment.
SETTINGS = {'scene': 'tangent', 'duration': 24, 'easing': 'smooth', 'count': 12, 'spread': 1, 'accent': '#B9A0FF', 'secondary': '#6EE7D8', 'background': '#101117', 'text': 'STAff', 'trail': True, 'quality': 'hd', 'fps': 60}

"""Parameterized studies for the project's pinned 3b1b/ManimGL runtime.

Exports embed SETTINGS before this file; no eval or uploaded code is used.
"""
import numpy as np
from manimlib import *

if 'SETTINGS' not in globals():
    SETTINGS = dict(scene='orbit', duration=6, easing='smooth', count=12,
                    spread=1, accent='#B9A0FF', secondary='#6EE7D8',
                    background='#101117', text='STAff', trail=True)


class StudioScene(Scene):
    def construct(self):
        s = SETTINGS
        self.camera.background_rgba = color_to_rgba(s['background'])
        self.accent, self.secondary = s['accent'], s['secondary']
        self.duration = s['duration']
        self.ease = {'smooth': smooth, 'linear': linear,
                     'rush_into': rush_into, 'rush_from': rush_from}[s['easing']]
        getattr(self, 'study_' + s['scene'])(s)

    def play_motion(self, *animations, portion=1):
        self.play(*animations, run_time=self.duration * portion, rate_func=self.ease)

    def study_orbit(self, s):
        radius = 1.9 * s['spread']
        rings = VGroup(*(Circle(radius=radius * r, stroke_color='#343545', stroke_width=1)
                         for r in [.5, 1, 1.25]))
        self.add(rings, Dot(radius=.09, fill_color=self.secondary))
        theta = ValueTracker(0)
        dots = VGroup()
        for i in range(s['count']):
            phase = TAU * i / s['count']
            r = radius * (.5 + .5 * (i % 3) / 2)
            dot = Dot(radius=.055 + .015 * (i % 3), fill_color=self.accent if i % 2 else self.secondary)
            dot.add_updater(lambda m, *, p=phase, r=r: m.move_to(
                r * np.array([np.cos(theta.get_value() + p), np.sin(theta.get_value() + p), 0])))
            dot.update()
            if s['trail']:
                self.add(TracedPath(dot.get_center, time_traced=self.duration * .35,
                                    stroke_color=dot.get_color(), stroke_width=2))
            dots.add(dot)
        self.add(dots)
        self.play_motion(theta.animate.set_value(TAU * 2))
        dots.clear_updaters()

    def study_morph(self, s):
        forms = [Circle(radius=1.7), Square(side_length=3),
                 RegularPolygon(n=3, radius=2), RegularPolygon(n=6, radius=1.9), Circle(radius=1.7)]
        for i, shape in enumerate(forms):
            shape.scale(s['spread']).set_stroke(self.accent if i % 2 == 0 else self.secondary, 3)
            shape.set_fill(shape.get_color(), opacity=.12)
        ghosts = Circle(radius=2.8, stroke_color='#30323F', stroke_width=1)
        self.add(ghosts, forms[0])
        active = forms[0]
        for target in forms[1:]:
            self.play_motion(Transform(active, target, path_arc=PI / s['count']), portion=.25)

    def study_wave(self, s):
        phase = ValueTracker(0)
        axes = VGroup(Line(LEFT * 5.4, RIGHT * 5.4), Line(DOWN * 2.8, UP * 2.8)).set_stroke('#343545', 1)
        self.add(axes)
        waves = VGroup()
        for i in range(s['count']):
            def wave(i=i):
                return ParametricCurve(lambda t: [t, s['spread'] * np.sin(t * 1.3 + phase.get_value() + i * .16), 0],
                                       t_range=(-5.3, 5.3, .04), stroke_color=self.accent if i % 2 else self.secondary,
                                       stroke_width=2, stroke_opacity=max(.15, 1 - i / s['count']))
            waves.add(always_redraw(wave))
        self.add(waves)
        self.play_motion(phase.animate.set_value(TAU * 2))
        waves.clear_updaters()

    def study_type(self, s):
        words = Text(s['text'], font='Helvetica', font_size=100, color=self.accent)
        words.set_width(min(9, words.get_width() * s['spread']))
        letters = list(words)
        self.play_motion(LaggedStart(*(FadeIn(m, shift=UP * .7) for m in letters),
                                     lag_ratio=1 / s['count']), portion=.35)
        self.play_motion(words.animate.set_color(self.secondary).scale(1.12), portion=.3)
        self.play_motion(LaggedStart(*(FadeOut(m, shift=DOWN * .7) for m in letters),
                                     lag_ratio=1 / s['count']), portion=.35)

    def study_burst(self, s):
        dots, targets = VGroup(), []
        for i in range(s['count']):
            angle = i * TAU / s['count']
            radius = s['spread'] * (1.4 + (i % 4) * .38)
            dot = Dot(radius=.07, fill_color=self.accent if i % 2 else self.secondary)
            dots.add(dot)
            targets.append(radius * np.array([np.cos(angle), np.sin(angle), 0]))
            if s['trail']:
                self.add(TracedPath(dot.get_center, time_traced=self.duration / 4,
                                    stroke_color=dot.get_color(), stroke_width=2))
        self.add(dots)
        self.play_motion(*(dot.animate.move_to(point) for dot, point in zip(dots, targets)), portion=.5)
        self.play_motion(*(dot.animate.move_to(ORIGIN) for dot in dots), portion=.5)

    def study_formula(self, s):
        # Fixed mathematical inputs keep the UI separate from arbitrary TeX execution.
        first = Tex(r'e^{i\theta}=\cos\theta+i\sin\theta', font_size=64,
                    isolate=[r'\theta'], t2c={r'\theta': self.accent})
        second = Tex(r'e^{i\pi}=\cos\pi+i\sin\pi', font_size=64,
                     isolate=[r'\pi'], t2c={r'\pi': self.secondary})
        final = Tex(r'e^{i\pi}+1=0', font_size=90, color=self.accent)
        for mob in [first, second, final]:
            mob.scale(s['spread'])
            if mob.get_width() > 11: mob.set_width(11)
        self.play_motion(Write(first, lag_ratio=1 / s['count']), portion=.3)
        self.play_motion(TransformMatchingTex(first, second, key_map={r'\theta': r'\pi'}), portion=.3)
        self.play_motion(TransformMatchingTex(second, final), portion=.3)
        self.wait(self.duration * .1)

    def study_tangent(self, s):
        """Experiment 01: explicit glyph ownership; no fading or auto matching.

        Stable symbols only translate. Changed terms use contour transforms.
        All source and target glyphs are accounted for at every substitution.
        """
        import json
        from pathlib import Path
        duration = self.duration
        ink, muted = '#EBEDF5', '#737D94'
        value, slope = self.secondary, self.accent
        timeline, audit = [], []
        elapsed = 0

        def play_for(seconds, *animations):
            nonlocal elapsed
            self.play(*animations, run_time=seconds * duration / 24, rate_func=self.ease)
            elapsed += seconds

        def hold(seconds):
            nonlocal elapsed
            self.wait(seconds * duration / 24)
            elapsed += seconds

        def mark(name):
            timeline.append(dict(stage=name, seconds=round(elapsed * duration / 24, 3)))

        title = Text('EXPERIMENT 01   /   THE TANGENT', font='Helvetica', font_size=19, color=muted)
        title.move_to([-3.65, 3.42, 0])
        rule = Line([-6.25, 3.05, 0], [6.25, 3.05, 0]).set_stroke('#303747', 1)
        tag = Text('SYMBOL  /  SUBSTITUTION  /  VALUE', font='Helvetica', font_size=13, color=muted)
        tag.move_to([3.88, 3.42, 0])
        self.add(title, rule, tag)

        strings = [r"y=f'(t)(x-t)+f(t)", r"y=f'(2)(x-2)+f(2)",
                   r"y=f'(2)(x-2)+4", r"y=4(x-2)+4"]
        equations = [Tex(tex, font_size=72, color=ink).move_to([0, 2.05, 0]) for tex in strings]
        # dvisvgm emits one path per visible glyph in these fixed expressions.
        expected = [17, 17, 14, 10]
        for equation, count in zip(equations, expected):
            if len(equation) != count:
                raise RuntimeError(f'Tangent glyph map changed: expected {count}, got {len(equation)}')
        for eq in equations[:3]:
            VGroup(*eq[2:7]).set_color(slope)
            eq[10].set_color(value)
        for eq in equations[:2]:
            eq[5].set_color(value)
            eq[15].set_color(value)
        equations[2][13].set_color(value)
        equations[3][2].set_color(slope)
        equations[3][6].set_color(value)
        equations[3][9].set_color(value)

        # Anchor all equations on the same mathematical baseline (not bbox centre).
        baseline = equations[0][0].get_center()[1]
        for eq in equations[1:]:
            eq.shift(UP * (baseline - eq[0].get_center()[1]))

        axes = Axes(x_range=(-.7, 3.5, 1), y_range=(-2, 10, 2), width=6.4, height=3.7,
                    axis_config=dict(stroke_color='#536078', stroke_width=1.3,
                                     include_tip=False, tick_size=.045))
        axes.move_to([-2.75, -1.22, 0])
        graph = axes.get_graph(lambda x: x*x, x_range=(-.6, 3.1, .025)).set_stroke('#8F9BAF', 2.5)
        function_label = Tex(r'f(x)=x^2', font_size=29, color=ink).set_color(ink).move_to([3.75, -.3, 0])
        derivative_label = Tex(r"f'(x)=2x", font_size=29, color=slope).set_color(slope).move_to([3.75, -.98, 0])
        x_label = Tex('x', font_size=23, color=muted).next_to(axes.x_axis.get_right(), RIGHT, buff=.15)
        y_label = Tex('y', font_size=23, color=muted).next_to(axes.y_axis.get_top(), UP, buff=.12)
        ticks = VGroup(*(Tex(str(n), font_size=19, color=muted).next_to(axes.c2p(n, 0), DOWN, buff=.12) for n in [1, 2, 3]))
        point_t = ValueTracker(.9)

        def tangent_endpoints():
            t = point_t.get_value()
            # Clip to the visible plot's y-range without letting x escape its range.
            lo, hi = max(-.6, (t*t-1.6)/(2*t)), min(3.25, (t*t+9.3)/(2*t))
            return axes.c2p(lo, 2*t*lo-t*t), axes.c2p(hi, 2*t*hi-t*t)

        tangent = Line(*tangent_endpoints()).set_stroke(slope, 2.8)
        point = Dot(axes.c2p(.9, .9**2), radius=.065, fill_color=value)
        guide = DashedLine(axes.c2p(.9, 0), point.get_center(), dash_length=.07).set_stroke(value, 1.1, opacity=.6)
        t_labels = [Tex(tex, font_size=29, color=value).set_color(value).move_to([3.75, -1.65, 0]) for tex in ['t', 't=2']]
        caption = Text('SYMBOLS TO VALUES', font='Helvetica', font_size=17).set_color(muted).move_to([0, .83, 0])
        play_for(2.6, Write(equations[0], lag_ratio=.045), Write(caption))
        mark('general_equation_written')
        play_for(2.2, ShowCreation(axes), ShowCreation(graph), Write(ticks), Write(x_label), Write(y_label),
                 Write(function_label), Write(derivative_label), Write(t_labels[0]))
        play_for(1.2, ShowCreation(tangent), GrowFromCenter(point), ShowCreation(guide))
        tangent.add_updater(lambda m: m.put_start_and_end_on(*tangent_endpoints()))
        point.add_updater(lambda m: m.move_to(axes.c2p(point_t.get_value(), point_t.get_value()**2)))
        guide.add_updater(lambda m: m.become(DashedLine(axes.c2p(point_t.get_value(), 0), point.get_center(),
                                                       dash_length=.07).set_stroke(value, 1.1, opacity=.6)))
        hold(1.0)
        mark('general_graph_drawn')

        def contour_transform(old, new, **kwargs):
            # Match closed contours by arc length, winding and starting point.
            # Default curve-index interpolation can fold a 2 into a tiny knot
            # on its way to 4 when font contours have different topology.
            samples = 128

            def sample_paths(mob):
                result = []
                for points in mob.get_subpaths():
                    curves = np.array([points[k:k+3] for k in range(0, len(points)-2, 2)])
                    u = np.linspace(0, 1, 18, endpoint=False)[None, :, None]
                    dense = ((1-u)**2*curves[:,0,None,:] + 2*u*(1-u)*curves[:,1,None,:]
                             + u**2*curves[:,2,None,:]).reshape(-1,3)
                    dense = np.vstack([dense, dense[0]])
                    distance = np.r_[0, np.cumsum(np.linalg.norm(np.diff(dense,axis=0),axis=1))]
                    stations = np.linspace(0, distance[-1], samples, endpoint=False)
                    result.append(np.column_stack([np.interp(stations,distance,dense[:,j]) for j in range(3)]))
                return sorted(result, key=lambda p: -abs(area(p)))

            def area(p):
                return np.sum(p[:,0]*np.roll(p[:,1],-1)-p[:,1]*np.roll(p[:,0],-1)) / 2

            aa, bb = sample_paths(old), sample_paths(new)
            if area(aa[0])*area(bb[0]) < 0:
                bb = [p[::-1] for p in bb]
            count = max(len(aa),len(bb))
            left, right = [], []
            for k in range(count):
                a = aa[k] if k<len(aa) else np.tile(bb[k].mean(axis=0)-new.get_center()+old.get_center(),(samples,1))
                b = bb[k] if k<len(bb) else np.tile(aa[k].mean(axis=0)-old.get_center()+new.get_center(),(samples,1))
                if k<len(aa) and k<len(bb):
                    centered_a = a-a.mean(axis=0)
                    centered_b = b-b.mean(axis=0)
                    shift = min(range(samples),key=lambda i: np.sum((centered_a-np.roll(centered_b,i,axis=0))**2))
                    b = np.roll(b,shift,axis=0)
                left.append(a);right.append(b)

            def quadratic_paths(paths):
                result=[]
                for p in paths:
                    q=np.empty((2*samples+1,3))
                    q[0:-1:2]=p;q[1::2]=(p+np.roll(p,-1,axis=0))/2;q[-1]=p[0]
                    if result: result.append(result[-1][-1:])
                    result.append(q)
                return np.vstack(result)

            old.set_points(quadratic_paths(left))
            target = new.copy().set_points(quadratic_paths(right))
            return Transform(old,target,path_arc=0,**kwargs)

        def transition(index, pairs, replacements, seconds, extras=()):
            source, target = equations[index], equations[index+1]
            src_seen, dst_seen, animations = [], [], []
            rigid_errors = []
            for a, b in pairs:
                # .shift, not a glyph transform: retained letters stay rigid.
                delta = target[b].get_center() - source[a].get_center()
                shape_error = float(np.max(np.abs(
                    source[a].get_points() - source[a].get_center() -
                    (target[b].get_points() - target[b].get_center()))))
                if shape_error > 1e-4:
                    raise RuntimeError(f'Retained glyph {a}->{b} is not a translation ({shape_error})')
                rigid_errors.append(shape_error)
                animations.append(source[a].animate.shift(delta))
                src_seen.append(a); dst_seen.append(b)
            for aa, bb in replacements:
                old = VGroup(*(source[k] for k in aa)) if len(aa)>1 else source[aa[0]]
                new = VGroup(*(target[k] for k in bb)) if len(bb)>1 else target[bb[0]]
                if len(aa) > 1 and len(bb) == 1:
                    # Keep the argument's numeral as the value's visual ancestor.
                    # Retract the function wrapper's strokes instead of piling
                    # multiple silhouettes onto one target digit.
                    anchor = aa[-2]
                    wrapper = VGroup(*(source[k] for k in aa if k != anchor))
                    animations.append(AnimationGroup(
                        Uncreate(wrapper, run_time=.65),
                        contour_transform(source[anchor], new, run_time=seconds),
                        lag_ratio=0))
                else:
                    animations.append(contour_transform(old, new))
                src_seen.extend(aa); dst_seen.extend(bb)
            if sorted(src_seen) != list(range(len(source))) or sorted(dst_seen) != list(range(len(target))):
                raise RuntimeError('Substitution mapping does not cover every glyph exactly once')
            play_for(seconds, *animations, *extras)
            # Consolidate ownership after the frame-identical transition endpoint.
            self.remove(source, *source.submobjects, *target.submobjects)
            self.add(target)
            audit.append(dict(source=strings[index], target=strings[index+1],
                              translated_glyphs=len(pairs), morphed_groups=len(replacements),
                              max_rigid_shape_error=max(rigid_errors, default=0),
                              all_glyphs_mapped=True, fades=0))

        # The small stage captions also contour-transform, with no dissolve.
        transition(0, [(i,i) for i in range(17) if i not in [5,10,15]],
                   [([i],[i]) for i in [5,10,15]], 2.8,
                   extras=(point_t.animate.set_value(2),
                           t_labels[0][0].animate.shift(t_labels[1][0].get_center() - t_labels[0][0].get_center()),
                           Write(t_labels[1][1:])))
        self.remove(t_labels[0], *t_labels[1].submobjects)
        self.add(t_labels[1])
        mark('t_equals_2')
        hold(1.5)
        height_label = Tex(r'f(2)=2^2=4', font_size=29, color=value).set_color(value).move_to([3.75, -2.3, 0])
        play_for(.8, Write(height_label))
        transition(1, [(i,i) for i in range(13)], [([13,14,15,16],[13])], 2.0)
        mark('function_value_4')
        hold(1.5)
        slope_value = Tex(r"f'(2)=2\cdot2=4", font_size=29, color=slope).set_color(slope).move_to([3.75, -2.95, 0])
        triangle = VGroup(Line(axes.c2p(2,4), axes.c2p(3,4)), Line(axes.c2p(3,4), axes.c2p(3,8))).set_stroke(slope, 1.5, opacity=.7)
        triangle_labels = VGroup(Tex('1', font_size=20, color=slope).next_to(triangle[0], DOWN, buff=.09),
                                Tex('4', font_size=20, color=slope).next_to(triangle[1], RIGHT, buff=.1))
        play_for(.9, Write(slope_value), ShowCreation(triangle), Write(triangle_labels))
        transition(2, [(0,0),(1,1)]+[(i,i-4) for i in range(7,14)], [([2,3,4,5,6],[2])], 2.0)
        mark('derivative_value_4')
        for mob in [tangent, point, guide]: mob.clear_updaters()
        final_box = SurroundingRectangle(equations[3], buff=.24).set_stroke(slope, 1.1, opacity=.6)
        play_for(.8, ShowCreation(final_box))
        hold(4.7)
        mark('final_hold')
        Path(self.file_writer.output_directory, 'tangent-audit.json').write_text(json.dumps(
            dict(experiment='01', function='x^2', t=2, timeline=timeline, transitions=audit,
                 no_fade_transitions=True, duration=duration), indent=2))
